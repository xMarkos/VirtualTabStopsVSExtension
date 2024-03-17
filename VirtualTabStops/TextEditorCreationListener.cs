using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace Markos.VirtualTabStops;

[Export(typeof(IVsTextViewCreationListener))]
[Name(nameof(TextEditorCreationListener))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal class TextEditorCreationListener : IVsTextViewCreationListener
{
    private readonly IServiceProvider _serviceProvider;

    [ImportingConstructor]
    public TextEditorCreationListener(SVsServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void VsTextViewCreated(IVsTextView textViewAdapter)
    {
        TabNavigationHandler.Create(_serviceProvider, textViewAdapter);
    }
}
