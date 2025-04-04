﻿using Avalonia;
using Avalonia.Interactivity;

using Beutl.Controls.PropertyEditors;
using Beutl.Media;

namespace Beutl.ViewModels.Editors;

public sealed class FontFamilyEditorViewModel(IPropertyAdapter<FontFamily?> property) : ValueEditorViewModel<FontFamily?>(property)
{
    public override void Accept(IPropertyEditorContextVisitor visitor)
    {
        base.Accept(visitor);
        if (visitor is FontFamilyEditor editor && !Disposables.IsDisposed)
        {
            editor.Bind(FontFamilyEditor.ValueProperty, Value.ToBinding())
                .DisposeWith(Disposables);
            editor.AddDisposableHandler(PropertyEditor.ValueConfirmedEvent, OnValueConfirmed)
                .DisposeWith(Disposables);
            editor.AddDisposableHandler(PropertyEditor.ValueChangedEvent, OnValueChanged)
                .DisposeWith(Disposables);
        }
    }

    private void OnValueChanged(object? sender, PropertyEditorValueChangedEventArgs e)
    {
        if (e is PropertyEditorValueChangedEventArgs<FontFamily?> args)
        {
            SetCurrentValueAndGetCoerced(args.NewValue);
        }
    }

    private void OnValueConfirmed(object? sender, PropertyEditorValueChangedEventArgs e)
    {
        if (e is PropertyEditorValueChangedEventArgs<FontFamily?> args)
        {
            SetValue(args.OldValue, args.NewValue);
        }
    }
}
