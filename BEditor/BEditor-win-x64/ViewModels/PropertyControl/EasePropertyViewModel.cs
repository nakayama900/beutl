﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEditor.ViewModels.Helper;

using BEditorCore.Data;
using BEditorCore.Data.PropertyData;
using BEditorCore.Data.PropertyData.EasingSetting;

namespace BEditor.ViewModels.PropertyControl {
    public class EasePropertyViewModel {
        public EaseProperty Property { get; }
        public DelegateCommand<EasingData> EasingChangeCommand { get; }

        public EasePropertyViewModel(EaseProperty property) {
            Property = property;
            EasingChangeCommand = new DelegateCommand<EasingData>(x => {
                UndoRedoManager.Do(new EaseProperty.ChangeEase(property, x.Name));
            });
        }
    }
}
