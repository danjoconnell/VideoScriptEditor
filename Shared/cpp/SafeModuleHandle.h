#pragma once

namespace VideoScriptEditor::Unmanaged
{
    // Based on 'HModule' sample code by Steven Engelhardt at https://www.stevenengelhardt.com/2005/09/09/use-raii/
    class SafeModuleHandle
    {
    private:
        HMODULE _moduleHandle = nullptr;

    public:
        explicit SafeModuleHandle(HMODULE moduleHandle) : _moduleHandle(moduleHandle) {}

        ~SafeModuleHandle()
        {
            if (_moduleHandle != nullptr)
            {
                FreeLibrary(_moduleHandle);
            }
        }

        operator HMODULE() const { return _moduleHandle; }

    private:
        // Disable copy construction and assignment
        SafeModuleHandle(const SafeModuleHandle& moduleHandle);
        SafeModuleHandle& operator=(const SafeModuleHandle& moduleHandle) {}
    };
}