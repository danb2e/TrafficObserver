using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibFFMPEG
{
    unsafe class FrameJPEGEncoder
    {
        //public AVFrame* buffer;
        string OutFilename;
        public FrameJPEGEncoder()
        {
        }
        public FrameJPEGEncoder(AVFrame data, string name)
        {
            //buffer = ffmpeg.av_frame_alloc();
            Size FrameSize = new Size(data.width, data.height);
            
            var vfc = new VideoFrameConverter(FrameSize, AVPixelFormat.AV_PIX_FMT_NV12, FrameSize, AVPixelFormat.AV_PIX_FMT_YUV420P);
            AVFrame convertedFrame = vfc.Convert(data);
            //*buffer = convertedFrame;
            //ffmpeg.av_frame_copy(buffer,&convertedFrame);
            OutFilename = name;
        }
        public void WriteImage(AVFrame data, string name)
        {
            Size FrameSize = new Size(data.width, data.height);

            var vfc = new VideoFrameConverter(FrameSize, AVPixelFormat.AV_PIX_FMT_NV12, FrameSize, AVPixelFormat.AV_PIX_FMT_YUVJ420P);
            AVFrame convertedFrame = vfc.Convert(data);
            //*buffer = convertedFrame;
            //ffmpeg.av_frame_copy(buffer, &convertedFrame);
            OutFilename = name;
            WriteImage(convertedFrame);

            vfc.Dispose();
        }
        public void WriteImage(AVFrame buffer)
        {
            AVFormatContext* pFormatCtx = ffmpeg.avformat_alloc_context();
            pFormatCtx->oformat = ffmpeg.av_guess_format("mjpeg", null, null);

            if (ffmpeg.avio_open(&pFormatCtx->pb, OutFilename, ffmpeg.AVIO_FLAG_READ_WRITE) < 0)
            {
                return;
            }

            AVStream* pAVStream = ffmpeg.avformat_new_stream(pFormatCtx, null);

            if (pAVStream == null)
            {
                return;
            }

            AVCodec* pCodec = ffmpeg.avcodec_find_encoder(pFormatCtx->oformat->video_codec);
            //pCodec->capabilities
            AVCodecContext* codecCtx = ffmpeg.avcodec_alloc_context3(pCodec);
            codecCtx->codec_id = pFormatCtx->oformat->video_codec;
            codecCtx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
            codecCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;
            codecCtx->width = buffer.width;
            codecCtx->height = buffer.height;
            codecCtx->time_base = new AVRational { num = 25, den = 1 };
            if (ffmpeg.av_hwdevice_ctx_create(&codecCtx->hw_device_ctx, AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA, null, null, 0) != 0)
            {
                return;
            }
            // Open the codec
            if (ffmpeg.avcodec_open2(codecCtx, pCodec, null) < 0)
            {
                return;
            }

            ffmpeg.avcodec_parameters_from_context(pAVStream->codecpar, codecCtx);
            ffmpeg.avformat_write_header(pFormatCtx, null);
            int y_size = (codecCtx->width) * (codecCtx->height);
            AVPacket pkt;
            ffmpeg.av_new_packet(&pkt, y_size);

            int ret = ffmpeg.avcodec_send_frame(codecCtx, &buffer);

            if (ret < 0)
            {
                return;
            }
            else
            {
                ret = ffmpeg.avcodec_receive_packet(codecCtx, &pkt);

                ret = ffmpeg.av_write_frame(pFormatCtx, &pkt);

            }

            ffmpeg.av_packet_unref(&pkt);
            //Write Trailer 
            ffmpeg.av_write_trailer(pFormatCtx);

            //printf("Encode Successful.\n");

            ffmpeg.avcodec_close(codecCtx);

            ffmpeg.avio_close(pFormatCtx->pb);

            ffmpeg.avformat_free_context(pFormatCtx);
            ffmpeg.avcodec_free_context(&codecCtx);

            return;
        }
    }
}
