﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BEditorCore {
    public static class Toolkit {
        private class disposable : IDisposable {
            public Action Action;

            public void Dispose() => Action?.Invoke();
        }

        public static IDisposable CreateDisposable(Action action) {
            return new disposable() { Action = action };
        }
    }
}
