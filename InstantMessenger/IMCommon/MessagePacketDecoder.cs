using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace IMCommon;

public class MessagePacketDecoder : ByteToMessageDecoder
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessagePacketDecoder>();

    enum E_DECODE_STAGE
    {
        HEADER = 0,
        CONTENTS
    }

    private E_DECODE_STAGE _stage = E_DECODE_STAGE.HEADER;
    private MessagePacket? _messagePacket;
    private readonly List<string> _headerList = new();

    private void InitLocalVars()
    {
        _stage = E_DECODE_STAGE.HEADER;
        _messagePacket = null;
        _headerList.Clear();
    }

    private MessagePacket TransformToHeader()
    {
        MessagePacket message = new();

        for (int i = 0; i < _headerList.Count; ++i)
        {
            var headerLine = _headerList[i];

            if (i == 0)
            {
                if (!headerLine.Equals("="))
                {
                    throw new ArgumentException($"첫번째 줄은 '='이어야 함({headerLine})");
                }
            }
            else if (i == 1)
            {
                // 두번째 라인 (action)
                message.SetAction(headerLine);
            }
            else if (i == _headerList.Count - 1)
            {
                // 마지막 라인
                if (!headerLine.Equals("="))
                {
                    throw new ArgumentException($"마지막 줄은 '='이어야 함({headerLine})");
                }
            }
            else
            {
                // 나머지 헤더
                int pos = headerLine.IndexOf(":");
                if (pos > 0)
                {
                    var key = headerLine[..pos].Trim();
                    var value = headerLine[(pos + 1)..].Trim();
                    if (key.Equals("length"))
                    {
                        message.SetLength(int.Parse(value));
                    }
                    else
                    {
                        message.AddHeader(key, value);
                    }
                }
            }
        }

        return message;
    }

    protected override void Decode(IChannelHandlerContext context, IByteBuffer input,
        List<object> output)
    {
        switch (_stage)
        {
            case E_DECODE_STAGE.HEADER:
                var line = PacketHelper.GetLine(input);
                if (string.IsNullOrEmpty(line))
                {
                    return;
                }

                _headerList.Add(line.Trim());

                // 두번째 "="인 row에서 header는 끝난다.
                if (_headerList.Count > 1 && line.Equals("="))
                {
                    _messagePacket = TransformToHeader();
                    if (_messagePacket.Length == 0)
                    {
                        output.Add(_messagePacket);
                        InitLocalVars();
                        return;
                    }
                    else
                    {
                        _stage = E_DECODE_STAGE.CONTENTS;
                    }
                }

                break;

            case E_DECODE_STAGE.CONTENTS:
                int length = _messagePacket.Length;
                if (input.ReadableBytes < length)
                {
                    break;
                }

                var byteBuffer = Unpooled.Buffer(length);
                input.ReadBytes(byteBuffer);

                _messagePacket.SetBody(byteBuffer.ToString(Encoding.UTF8));
                byteBuffer.Release();

                output.Add(_messagePacket);
                InitLocalVars();
                break;
        }
    }
}
