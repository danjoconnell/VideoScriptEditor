#pragma once

/// <summary>
/// A clip consisting of a single frame.
/// </summary>
class SingleFrameClip : public IClip
{
private:
    VideoInfo vi;
    PVideoFrame videoFrame;

public:
    SingleFrameClip(const VideoInfo& _vi, const PVideoFrame& _videoFrame)
        : vi(_vi), videoFrame(_videoFrame)
    {
    }

    PVideoFrame __stdcall GetFrame(int n, IScriptEnvironment* env)
    {
        return videoFrame;
    }

    void __stdcall GetAudio(void* buf, __int64 start, __int64 count, IScriptEnvironment* env)
    {
    }

    const VideoInfo& __stdcall GetVideoInfo()
    {
        return vi;
    }

    bool __stdcall GetParity(int n) 
    {
        return false;
    }

    int __stdcall SetCacheHints(int cachehints, int frame_range)
    {
        return 0;
    };
};