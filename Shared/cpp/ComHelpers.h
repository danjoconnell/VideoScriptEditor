#pragma once

namespace HR
{
    /// <summary>
    /// Throws a <see cref="_com_error"/> exception if the <see cref="HRESULT"/> value represents a failure code.
    /// </summary>
    /// <param name="hr">The <see cref="HRESULT"/> value to check for failure.</param>
    inline void ThrowIfFailed(HRESULT hr)
    {
        if (FAILED(hr))
        {
            _com_raise_error(hr);
        }
    }
}