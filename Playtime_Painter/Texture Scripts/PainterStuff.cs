﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    public delegate bool PainterBoolPlugin(PlaytimePainter p);

    [Serializable]
    public abstract class PainterStuffKeepUnrecognized_STD : PainterStuff_STD, iKeepUnrecognizedSTD  {
  
        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();

        public void Unrecognized(string tag, string data) {
            this.Unrecognized(tag, data, ref unrecognizedTags, ref unrecognizedData);
        }
        
        public StdEncoder EncodeUnrecognized()
        {
            var cody = new StdEncoder();
            for (int i = 0; i < unrecognizedTags.Count; i++)
                cody.Add_String(unrecognizedTags[i], unrecognizedData[i]);
            return cody;
        }
#if PEGI
        public static int inspectedUnrecognized = -1;
        public override bool PEGI()
        {
            bool changed = false;
            if (unrecognizedTags.Count > 0)
            {
                "Unrecognized Tags".nl();
                for (int i = 0; i < unrecognizedTags.Count; i++)
                {
                    if (icon.Delete.Click())
                    {
                        changed = true;
                        unrecognizedTags.RemoveAt(i);
                        unrecognizedData.RemoveAt(i);
                        i--;
                    }
                    else if (unrecognizedTags[i].foldout(ref inspectedUnrecognized, i).nl())
                        unrecognizedData[i].nl();
                }
            }

            return changed;
        }
#endif
    }

    [Serializable]
    public abstract class PainterStuff_STD : PainterStuff, iSTD
#if PEGI
        , IPEGI
#endif
    {
    public abstract StdEncoder Encode();

        public iSTD Decode(string data) => data.DecodeInto(this);

        public iSTD Decode(StdEncoder cody)  => new StdDecoder(cody.ToString()).DecodeTagsFor(this);

#if PEGI
        public virtual bool PEGI() { pegi.nl(); (GetType() + " class has no PEGI() function.").nl(); return false; }
#endif
        public abstract bool Decode(string tag, string data);
        public abstract string GetDefaultTagName();
    }


    public class PainterStuffScriptable : ScriptableObject
    {

        protected static PainterManager texMGMT { get { return PainterManager.inst; } }
        protected static Transform rtbrush { get { return texMGMT.brushRendy.transform; } }
        protected static Mesh brushMesh { set { texMGMT.brushRendy.meshFilter.mesh = value; } }
        protected static PainterConfig cfg { get { return PainterConfig.inst; } }
        protected static BrushConfig inspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig globalBrush { get { return cfg.brushConfig; } }
        protected static PlaytimePainter inspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageData inspectedImageData { get { var ip = inspectedPainter; return ip ? ip.imgData : null; } }
        protected static GridNavigator grid { get { return GridNavigator.inst(); } }
        protected static MeshManager meshMGMT { get { return MeshManager.Inst; } }
        protected static EditableMesh editedMesh { get { return MeshManager.Inst.edMesh; } }
        protected static bool applicationIsQuitting { get { return PainterStuff.applicationIsQuitting; }  }
        protected static bool isNowPlaytimeAndDisabled { get { return PainterStuff.isNowPlaytimeAndDisabled; } }
    }

    public class PainterStuffMono : MonoBehaviour
    {

        protected static PainterManager texMGMT { get { return PainterManager.inst; } }
        protected static Transform rtbrush { get { return texMGMT.brushRendy.transform; } }
        protected static Mesh brushMesh { set { texMGMT.brushRendy.meshFilter.mesh = value; } }
        protected static PainterConfig cfg { get { return PainterConfig.inst; } }
        protected static BrushConfig inspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig globalBrush { get { return cfg.brushConfig; } }
        protected static PlaytimePainter inspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageData inspectedImageData { get { var ip = inspectedPainter; return ip != null ? ip.imgData : null; } }
        protected static GridNavigator grid { get { return GridNavigator.inst(); } }
        protected static MeshManager meshMGMT { get { return MeshManager.Inst; } }
        protected static EditableMesh editedMesh { get { return MeshManager.Inst.edMesh; } }
        protected static bool applicationIsQuitting { get { return PainterStuff.applicationIsQuitting; }  }
        protected static bool isNowPlaytimeAndDisabled { get { return PainterStuff.isNowPlaytimeAndDisabled; } }
        
        }


    [Serializable]
    public class PainterStuff {

        protected static PainterManager texMGMT { get { return PainterManager.inst; } }
        protected static Transform rtbrush { get { return texMGMT.brushRendy.transform; } }
        protected static Mesh brushMesh { set { texMGMT.brushRendy.meshFilter.mesh = value; } }
        protected static PainterConfig cfg { get { return PainterConfig.inst; } }
        protected static BrushConfig inspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig globalBrush { get { return cfg.brushConfig; }  }
        protected static PlaytimePainter inspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageData inspectedImageData { get { var ip = inspectedPainter; return ip ? ip.imgData : null; } }
        protected static GridNavigator grid { get { return GridNavigator.inst(); } }
        protected static MeshManager meshMGMT { get { return MeshManager.Inst; } }
        protected static EditableMesh editedMesh { get { return MeshManager.Inst.edMesh; } }
        public static bool applicationIsQuitting;

        public static bool isNowPlaytimeAndDisabled { get
            {
#if !BUILD_WITH_PAINTER
                if (Application.isPlaying)
                    return true;
#endif
                return false;
            }
        }

    }

}