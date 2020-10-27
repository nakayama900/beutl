﻿using System.Collections.Generic;
using System.Runtime.Serialization;

using BEditorCore;
using BEditorCore.Data.ObjectData;
using BEditorCore.Data.ProjectData;
using BEditorCore.Data.PropertyData;
using BEditorCore.Media;

namespace BEditorCore.Data.EffectData {

    [DataContract(Namespace = "")]
    public class Dilate : ImageEffect {

        public static readonly EasePropertyMetadata FrequencyMetadata = new EasePropertyMetadata(Properties.Resources.Frequency, 1, float.NaN, 0);


        public Dilate() {
            Frequency = new EaseProperty(FrequencyMetadata);
        }

        #region EffectProperty
        public override string Name => Properties.Resources.Dilate;

        public override void Draw(ref Image source, EffectLoadArgs args) => source.Dilate((int)Frequency.GetValue(args.Frame));


        public override IList<PropertyElement> PropertySettings => new List<PropertyElement> { Frequency };

        public override void PropertyLoaded() {
            base.PropertyLoaded();

            Frequency.PropertyMetadata = FrequencyMetadata;
        }

        #endregion


        [DataMember(Order = 0)]
        public EaseProperty Frequency { get; set; }
    }
}
