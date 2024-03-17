using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace Markos.VirtualTabStops;

internal abstract class CommandListener : IOleCommandTarget
{
    private IOleCommandTarget _nextTarget;

    public virtual void RegisterInto(IVsTextView view)
    {
        if (view.AddCommandFilter(this, out _nextTarget) != VSConstants.S_OK)
            throw new InvalidOperationException("Command registration failed.");
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
        if (_nextTarget == null)
            throw new InvalidOperationException();

        ThreadHelper.ThrowIfNotOnUIThread();
        return _nextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        if (_nextTarget == null)
            throw new InvalidOperationException();

        if (ExecuteCommand(ref pguidCmdGroup, nCmdID))
            return 0;

        ThreadHelper.ThrowIfNotOnUIThread();
        return _nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
    }

    protected abstract bool ExecuteCommand(ref Guid cmdGroup, uint cmdId);
}
