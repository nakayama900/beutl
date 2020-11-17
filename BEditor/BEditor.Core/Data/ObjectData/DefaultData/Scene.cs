﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using BEditor.Core.Data.ProjectData;
using BEditor.Core.Data.PropertyData;
using BEditor.Core.Extensions;
using BEditor.Core.Media;

namespace BEditor.Core.Data.ObjectData
{
    public static partial class DefaultData
    {
        [DataContract(Namespace = "")]
        public class Scene : DefaultImageObject
        {
            SelectorPropertyMetadata SelectSceneMetadata;

            #region DefaultImageObjectメンバー

            public override IEnumerable<PropertyElement> Properties => new PropertyElement[]
            {
                Start,
                SelectScene
            };

            public override Media.Image Render(EffectRenderArgs args)
            {
                ProjectData.Scene scene = SelectScene.SelectItem as ProjectData.Scene;
                // Clipの相対的なフレーム
                var frame = args.Frame - Parent.Parent.Start;

                return scene.Render(frame + (int)Start.GetValue(args.Frame)).Image;
            }

            public override void PropertyLoaded()
            {
                SelectSceneMetadata = new ScenesSelectorMetadata(this);
                Start.ExecuteLoaded(Video.StartMetadata);
                SelectScene.ExecuteLoaded(SelectSceneMetadata);
            }

            #endregion

            public Scene()
            {
                Start = new(Video.StartMetadata);

                // この時点で親要素を取得できないので適当なデータを渡す
                SelectScene = new(new SelectorPropertyMetadata("", new string[1]));
            }

            [DataMember(Order = 0)]
            public EaseProperty Start { get; private set; }

            [DataMember(Order = 1)]
            public SelectorProperty SelectScene { get; private set; }


            internal record ScenesSelectorMetadata : SelectorPropertyMetadata
            {
                internal ScenesSelectorMetadata(Scene scene) : base(Core.Properties.Resources.Scenes, null)
                {
                    MemberPath = "SceneName";
                    ItemSource = scene
                        .Parent
                        .Parent
                        .Parent
                        .Parent
                        .SceneList
                        .Where(scene1=>scene1 != scene.Parent.Parent.Parent)
                        .ToList();
                }
            }
        }
    }
}
