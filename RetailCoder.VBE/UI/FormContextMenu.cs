﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Office.Core;
using Microsoft.Vbe.Interop;
using Rubberduck.Parsing;
using Rubberduck.Refactorings.Rename;
using Rubberduck.UI.Refactorings;
using Rubberduck.VBEditor;
using Rubberduck.VBEditor.VBEInterfaces.RubberduckCodePane;

namespace Rubberduck.UI
{
    internal class FormContextMenu
    {
        private readonly IRubberduckParser _parser;
        private readonly IActiveCodePaneEditor _editor;
        private readonly VBE _vbe;
        private readonly IRubberduckCodePaneFactory _factory;

        // ReSharper disable once NotAccessedField.Local
        private CommandBarButton _rename;

        public FormContextMenu(VBE vbe, IRubberduckParser parser, IActiveCodePaneEditor editor, IRubberduckCodePaneFactory factory)
        {
            _vbe = vbe;
            _parser = parser;
            _editor = editor;
            _factory = factory;
        }

        public void Initialize()
        {
            var beforeItem = _vbe.CommandBars["MSForms Control"].Controls.Cast<CommandBarControl>().First(control => control.Id == 2558).Index;
            _rename = _vbe.CommandBars["MSForms Control"].Controls.Add(Type: MsoControlType.msoControlButton, Temporary: true, Before: beforeItem) as CommandBarButton;
            _rename.BeginGroup = true;
            _rename.Caption = RubberduckUI.FormContextMenu_Rename;
            _rename.Click += OnRenameButtonClick;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private void OnRenameButtonClick(CommandBarButton Ctrl, ref bool CancelDefault)
        {
            Rename();
        }

        private void Rename()
        {
            var progress = new ParsingProgressPresenter();
            var result = progress.Parse(_parser, _vbe.ActiveVBProject);

            var designer = (dynamic) _vbe.SelectedVBComponent.Designer;

            foreach (var control in designer.Controls)
            {
                if (!control.InSelection) { continue; }

                var controlToRename =
                    result.Declarations.Items
                        .FirstOrDefault(item => item.IdentifierName == control.Name
                                                && item.ComponentName == _vbe.SelectedVBComponent.Name
                                                && _vbe.ActiveVBProject.Equals(item.Project));

                using (var view = new RenameDialog())
                {
                    var factory = new RenamePresenterFactory(_vbe, view, result, new RubberduckMessageBox(), _factory);
                    var refactoring = new RenameRefactoring(factory, _editor, new RubberduckMessageBox());
                    refactoring.Refactor(controlToRename);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) { return; }

            if (_rename != null)
            {
                _rename.Click -= OnRenameButtonClick;
                _rename.Delete();
            }
        }
    }
}
