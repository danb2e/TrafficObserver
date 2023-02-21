using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Drawing;
using System.Data;

namespace LibFFMPEG
{
    public unsafe class LibFFMPEG
    {
        AVHWDeviceType HWDevice;
        VideoStreamDecoder vsd;
        VideoFrameConverter vfc;
        int NumFrame;
        string input, output;
        FrameJPEGEncoder EncoderJPEG;

        public LibFFMPEG()
        {
            ffmpeg.RootPath = Path.Combine(Environment.CurrentDirectory, "DLLs");
            EncoderJPEG = new FrameJPEGEncoder();
            NumFrame = -1;
           
        }
        public void Dispose()
        {
            if(vsd != null)
                vsd.Dispose();
            if (vfc != null)
                vfc.Dispose();

            vsd = null;
            vfc = null;
        }
        public void SetDecoderCUDA(string InputPath, string OutputPath)
        {
            input = InputPath; output = OutputPath; 
            AVHWDeviceType HWDevice = AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA;

            vsd = new VideoStreamDecoder(input, HWDevice);

            vfc = new VideoFrameConverter(vsd.FrameSize, AVPixelFormat.AV_PIX_FMT_NV12, vsd.FrameSize, AVPixelFormat.AV_PIX_FMT_YUV420P);

            NumFrame = 0;
        }
        public void SetDecoderDefault(string InputPath, string OutputPath)
        {
            input = InputPath; output = OutputPath;
            AVHWDeviceType HWDevice = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;

            vsd = new VideoStreamDecoder(input, HWDevice);

            vfc = new VideoFrameConverter(vsd.FrameSize, AVPixelFormat.AV_PIX_FMT_NV12, vsd.FrameSize, AVPixelFormat.AV_PIX_FMT_YUV420P);

            NumFrame = 0;
        }

        public string DecodeNextFrame()
        {
            if (NumFrame == -1)
                return "";

            string outputFile = "";
            AVFrame frame;

            if (vsd.TryDecodeNextFrame(out frame))
            {
                AVFrame convertedFrame = new AVFrame();
                convertedFrame = vfc.Convert(frame);
                outputFile = Path.Combine(output, NumFrame.ToString("D10") + ".jpg");

                EncoderJPEG.WriteImage(frame, outputFile);
                
                NumFrame++;
            }
            return outputFile;
        }
    }
}
