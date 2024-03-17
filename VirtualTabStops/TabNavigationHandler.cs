using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Linq;

using Caret = (int Line, int Column);
using TabStop = (int Left, int Right);

namespace Markos.VirtualTabStops;

/// <summary>
/// Handler for <see cref="ITextView"/> navigation events.
/// </summary>
internal class TabNavigationHandler : CommandListener
{
    private readonly IVsTextView _viewAdapter;
    private readonly IWpfTextView _view;

    public int TabSize => _view.FormattedLineSource.TabSize;

    public Caret Caret
    {
        get
        {
            _viewAdapter.GetCaretPos(out int line, out int column);
            return (line, column);
        }
    }

    public TabStop NearestTabStop => GetNearestTabStop(Caret.Column, TabSize);

    private TabNavigationHandler(IVsTextView viewAdapter, IWpfTextView view)
    {
        _viewAdapter = viewAdapter ?? throw new ArgumentNullException(nameof(viewAdapter));
        _view = view ?? throw new ArgumentNullException(nameof(view));
    }

    public static TabNavigationHandler Create(IServiceProvider provider, IVsTextView viewAdapter)
    {
        IComponentModel componentModel = provider.GetService<SComponentModel, IComponentModel>();
        IVsEditorAdaptersFactoryService editorFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();

        TabNavigationHandler handler = new(viewAdapter, editorFactory.GetWpfTextView(viewAdapter));
        handler.RegisterInto(viewAdapter);
        return handler;
    }

    protected override bool ExecuteCommand(ref Guid cmdGroup, uint cmdId)
    {
        NavigationEvent @event = default;

        if (cmdGroup == VSConstants.VSStd2K)
        {
            @event = (VSConstants.VSStd2KCmdID)cmdId switch
            {
                VSConstants.VSStd2KCmdID.LEFT => NavigationEvent.Left,
                VSConstants.VSStd2KCmdID.RIGHT => NavigationEvent.Right,
                VSConstants.VSStd2KCmdID.LEFT_EXT => NavigationEvent.ExtendLeft,
                VSConstants.VSStd2KCmdID.RIGHT_EXT => NavigationEvent.ExtendRight,
                VSConstants.VSStd2KCmdID.BACKSPACE => NavigationEvent.Backspace,
                VSConstants.VSStd2KCmdID.DELETE => NavigationEvent.Delete,
                _ => default,
            };
        }
        else if (cmdGroup == VSConstants.GUID_VSStandardCommandSet97 && cmdId == (uint)VSConstants.VSStd97CmdID.Delete)
        {
            @event = NavigationEvent.Delete;
        }

        if (@event != NavigationEvent.Unknown)
            return HandleNavigationEvent(@event);

        return false;
    }

    private bool HandleNavigationEvent(NavigationEvent @event)
    {
        Caret caret = Caret;
        TabStop nextTab = GetNearestTabStop(caret.Column, TabSize);
        bool atTabStop = IsTabStop(caret.Column, TabSize);
        int currentTabSize = 0;
        int nextTabColumn = 0;
        string buffer = null;

        if (@event is NavigationEvent.Left or NavigationEvent.ExtendLeft or NavigationEvent.Backspace)
        {
            if (_viewAdapter.GetTextStream(caret.Line, nextTab.Left, caret.Line, caret.Column, out buffer) == VSConstants.S_OK)
            {
                currentTabSize = buffer.Reverse().TakeWhile(x => x == ' ').Count();
                nextTabColumn = caret.Column - currentTabSize;
            }
        }
        else if (@event is NavigationEvent.Right or NavigationEvent.ExtendRight or NavigationEvent.Delete)
        {
            if (_viewAdapter.GetTextStream(caret.Line, caret.Column, caret.Line, nextTab.Right, out buffer) == VSConstants.S_OK)
            {
                currentTabSize = buffer.TakeWhile(x => x == ' ').Count();
                nextTabColumn = caret.Column + currentTabSize;
            }
        }

        bool isVTab = buffer != null && (currentTabSize == buffer.Length || (atTabStop && currentTabSize > 1));

        if (isVTab)
        {
            switch (@event)
            {
                case NavigationEvent.Left:
                case NavigationEvent.Right:
                    NavigateToTab();
                    break;
                case NavigationEvent.Backspace:
                case NavigationEvent.Delete:
                    DeleteTab();
                    break;
                case NavigationEvent.ExtendLeft:
                case NavigationEvent.ExtendRight:
                    ExtendSelectionToTab();
                    break;
                default:
                    return false;
            }

            return true;
        }

        return false;

        void NavigateToTab()
            => _viewAdapter.SetCaretPos(caret.Line, nextTabColumn);

        void DeleteTab()
            => _viewAdapter.ReplaceTextOnLine(caret.Line, Math.Min(nextTabColumn, caret.Column), currentTabSize, "", 0);

        void ExtendSelectionToTab()
        {
            bool hasSelection = _viewAdapter.GetSelection(out int anchorLine, out int anchorCol, out int endLine, out int endCol) == VSConstants.S_OK;
            if (hasSelection)
            {
                _viewAdapter.SetSelection(anchorLine, anchorCol, endLine, nextTabColumn);
            }
            else
            {
                _viewAdapter.SetSelection(caret.Line, caret.Column, caret.Line, nextTabColumn);
            }
        }
    }

    public static TabStop GetNearestTabStop(int column, int tabSize)
    {
        int left = column % tabSize;
        int right = (tabSize - left) % tabSize;

        if (left == 0)
            left = tabSize;

        if (right == 0)
            right = tabSize;

        return (column - left, column + right);
    }

    public static bool IsTabStop(int column, int tabSize)
        => column % tabSize == 0;

    private enum NavigationEvent
    {
        Unknown = 0,
        Left,
        Right,
        ExtendLeft,
        ExtendRight,
        Backspace,
        Delete,
    }
}
