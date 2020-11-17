#include "pch.h"
#include "ScriptVideoContext.h"
#include <msclr/lock.h>

using namespace System;
using namespace VideoScriptEditor::Models::Primitives;

namespace VideoScriptEditor::Services::ScriptVideo
{
    ScriptVideoContext::ScriptVideoContext(IScriptVideoService^ scriptVideoService, Services::Dialog::ISystemDialogService^ systemDialogService)
        : ScriptVideoContextBase(scriptVideoService, systemDialogService), _applyMaskingPreviewToSourceRender(false)
    {
    }

    bool ScriptVideoContext::ApplyMaskingPreviewToSourceRender::get()
    {
        return _applyMaskingPreviewToSourceRender;
    }

    void ScriptVideoContext::ApplyMaskingPreviewToSourceRender::set(bool value)
    {
        msclr::lock clrLock(_syncLock);
        SetProperty(_applyMaskingPreviewToSourceRender, value, NAMEOF(ApplyMaskingPreviewToSourceRender));
    }

    void ScriptVideoContext::SetVideoPropertiesFromUnmanagedStruct(const PreviewRenderer::Unmanaged::LoadedScriptVideoInfo& loadedScriptVideoInfo)
    {
        using System::Drawing::Size;

        HasVideo = loadedScriptVideoInfo.HasVideo;
        VideoFrameSize = Size(loadedScriptVideoInfo.PixelWidth, loadedScriptVideoInfo.PixelHeight);
        VideoFrameCount = loadedScriptVideoInfo.FrameCount;
        SeekableVideoFrameCount = Math::Max(_videoFrameCount - 1, 0);
        VideoFramerate = Fraction(static_cast<int>(loadedScriptVideoInfo.FpsNumerator), static_cast<int>(loadedScriptVideoInfo.FpsDenominator));
        VideoDuration = TimeSpan::FromTicks(TimeSpan::TicksPerSecond * _videoFrameCount * _videoFramerate.Denominator / _videoFramerate.Numerator);

        AspectRatio = Ratio(
            _videoFrameSize.Width > 0 ? _videoFrameSize.Width : 1,  // numerator
            _videoFrameSize.Height > 0 ? _videoFrameSize.Height : 1,// denominator
            true                                                    // simplify
        );
    }

    void ScriptVideoContext::SetScriptFileSourceInternal(System::String^ scriptFileSource)
    {
        msclr::lock clrLock(_syncLock);
        SetProperty(_scriptFileSource, scriptFileSource, NAMEOF(ScriptFileSource));
    }
}