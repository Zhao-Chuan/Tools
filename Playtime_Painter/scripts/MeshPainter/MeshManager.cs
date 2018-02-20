﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PlayerAndEditorGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Painter
{
 
    //  public enum meshSHaderMode { lit = "MESH_PREVIEW_LIT",  MESH_PREVIEW_NORMAL MESH_PREVIEW_VERTCOLOR MESH_PREVIEW_PROJECTION }

	[Serializable]
    [ExecuteInEditMode]
    public class MeshManager {


        public static MeshManager inst { get
            {
                return PainterManager.inst.meshManager;
            }
        }

        public static Transform transform { get { return PainterManager.inst.transform; } }

        public static GridNavigator grid { get { return GridNavigator.inst(); } }

        public static float animTextureSize = 128;

        public const string ToolName = "Mesh_Editor";

        public static string ToolPath()
        {
			return PlaytimeToolComponent.ToolsFolder + "/" + ToolName;
        }

        public static MeshTool _meshTool {
            get
            {
                return cfg._meshTool;
            }
        } // will be depricated

        public static MeshToolBase tool { get { return PainterConfig.inst._meshTool.Get(); } }

        public static PainterConfig cfg {
            get {
                return PainterConfig.inst;
            }
        }
        public static int curUV = 0;
        public bool showGrid = true;
        public static Vector3 editorMousePos;

        public static BrushConfig brushConfig() {
            return cfg.brushConfig;
        }
      

        [NonSerialized]
        public PlaytimePainter target;
        public PlaytimePainter previouslyEdited;

        [NonSerialized]
        public EditableMesh _Mesh = new EditableMesh();
        [NonSerialized]
        public EditableMesh _PreviewMeshGen = new EditableMesh();
        [NonSerialized]
        public Mesh _PreviewMesh;

        //[NonSerialized]
        public AddCubeCfg tmpCubeCfg = new AddCubeCfg();
      
        public G_tool G_toolDta = new G_tool();

        public float outlineWidth = 1;
        int currentUV = 0;
        bool SelectingUVbyNumber = false;
        public bool GridToUVon = false;

      

        [NonSerialized]
        public UVpoint selectedUV;
        [NonSerialized]
        public LineData selectedLine;
        [NonSerialized]
        public trisDta selectedTris;

        [NonSerialized]
        public UVpoint pointedUV;
        [NonSerialized]
        public LineData pointedLine;
        [NonSerialized]
        public trisDta pointedTris;

		[NonSerialized]
        UVpoint[] TrisSet = new UVpoint[3];
        public int trisVerts = 0;
		[NonSerialized]
        public int vertsShowMax = 8;

        [NonSerialized]
        public Vector3 onGridLocal = new Vector3();
        [NonSerialized]
        public Vector3 collisionPosLocal = new Vector3();

        public void UpdateLocalSpaceV3s() {
            if (target != null) {
                onGridLocal = target.transform.InverseTransformPoint(GridNavigator.onGridPos);
                collisionPosLocal = target.transform.InverseTransformPoint(GridNavigator.collisionPos);
            }
        }

        public void TextureAnim_ToCollider() {
            _PreviewMeshGen.CopyFrom(_Mesh);
            _PreviewMeshGen.AddTextureAnimDisplacement();
            MeshConstructor con = new MeshConstructor(_Mesh, target.meshProfile, null);
            con.AssignMeshAsCollider(target.meshCollider );
        }

        public void EditMesh(PlaytimePainter painter, bool EditCopy) {
            if ((painter == null) || (painter == target))
                return;

            if (target != null)
                DisconnectMesh();

            target = painter;


            _Mesh.Edit(painter);

			/*if (painter.gotMeshData ()) {
				_Mesh.Reboot (painter.lastMeshSavedDta);
				if (_Mesh.triangles.Count == 0)
					_Mesh.BreakMesh (painter.meshFilter.sharedMesh);

			} else {
				_Mesh.BreakMesh (painter.meshFilter.sharedMesh);
				painter.selectedMeshProfile = painter.getMaterial (false).getMeshProfileByTag ();
			}*/

            if (EditCopy)
                painter.meshFilter.sharedMesh = new Mesh();

            // MeshConstructionData mc = new MeshConstructionData(_Mesh, cfg.meshProfiles[0]);
            Redraw();

            painter.meshNameHolder = _Mesh.meshName;

            InitVertsIfNUll();

            selectedLine = null;
            selectedTris = null;
            selectedUV = null;
        }

        public void DisconnectMesh() {
            if (target!= null)
            target.meshSaveData = _Mesh.Encode().ToString();
            target = null;
            grid.Deactivateverts();
            GridNavigator.inst().SetEnabled(false, false);

        }

#if UNITY_EDITOR
        public void SaveGeneratedMeshAsAsset()
        {
            AssetDatabase.CreateAsset(target.meshFilter.mesh, "Assets/Models/" + target.gameObject.name + "_export.asset");
            AssetDatabase.SaveAssets();
        }
#endif

        public void Redraw() {
            _Mesh.RefresVerticleTrisList();

            _Mesh.Dirty = false;
            if (target != null) {
                MeshConstructor mc = new MeshConstructor(_Mesh, target.meshProfile, target.meshFilter.sharedMesh);
                target.meshFilter.sharedMesh = mc.mesh;
                
                mc.AssignMeshAsCollider(target.meshCollider);
            }

            if (_meshTool == MeshTool.VertexAnimation)
            {
                //UpdateAnimations(SaveAbleMesh sbm);
              /*  int curFrame = _target.GetAnimationUVy();//getBaseDependencies().stretch_Monitor.curUVy;
                MeshConstructionData svm = _target.saveMeshDta;
                vertexAnimationFrame vaf = svm.anims[curFrame];
                if (vaf != null)
                {
                    foreach (vertexpointDta vp in _Mesh.vertices)
                        vaf.verts[ vp.index] = vp.anim[curFrame];

                    svm.updateAnimation(curFrame);
                }
*/
            }

            //  Debug.Log("Redraw ");
        }

        public static Vector2 RoundUVs(Vector2 source, float accuracy)
        {
            Vector2 uv = source * accuracy;
            uv.x = Mathf.Round(uv.x);
            uv.y = Mathf.Round(uv.y);
            uv /= accuracy;
            return uv;
        }

        public void AddToTrisSet(UVpoint nuv)
        {

            TrisSet[trisVerts] = nuv;
            trisVerts++;

            if (trisVerts == 3)
            {
                foreach (trisDta t in _Mesh.triangles)
                {
                    if (t.IsSamePoints(TrisSet))
                    {
                        t.Change(TrisSet);
                        _Mesh.Dirty = true;
                        trisVerts = 0;
                        return;
                    }
                }
            }

            if (trisVerts >= 3)
            {
                trisDta td = new trisDta(TrisSet);
                _Mesh.triangles.Add(td);

                if (!EditorInputManager.getControlKey())
                {
                    MakeTriangleVertUnique(td, TrisSet[0]);
                    MakeTriangleVertUnique(td, TrisSet[1]);
                    MakeTriangleVertUnique(td, TrisSet[2]);
                }

                trisVerts = 0;
                _Mesh.Dirty = true;
            }
        }
        public void ProcessScaleChange()
        {
            if ((_Mesh.vertices == null) || (_Mesh.vertices.Count < 1)) return;
            if ((_meshTool == MeshTool.uv) && (GridToUVon))
            {
                if (target != null)
                    _PreviewMeshGen.CopyFrom(_Mesh);

                if (selectedUV == null) selectedUV = _Mesh.vertices[0].uv[0];
                foreach (vertexpointDta v in _PreviewMeshGen.vertices)
                {
                    foreach (UVpoint uv in v.uv)
                    {
                        uv.uv = PosToUV(v.pos);
                    }
                }

               // _PreviewMeshGen.GenerateMeshAndAssign(_target.saveMeshDta);
            }
        }

        public void UpdatePreviewIfGridedDraw()
        {
            if (target != null)
            {
                if ((_meshTool == MeshTool.uv) && (GridToUVon))
                {
                    ProcessScaleChange();
                }
                else
                    _Mesh.Dirty = true;
            }
        }

        public Vector2 PosToUV(Vector3 pos2)
        {

            Vector2 uv = new Vector2();
            Vector3 diff = (pos2 - selectedUV.vert.pos) / Mathf.Max(0.01f, cfg.MeshUVprojectionSize);

            switch (GridNavigator.inst().g_side)
            {
                case Gridside.xy:
                    uv.x = diff.x;
                    uv.y = diff.y;
                    break;
                case Gridside.xz:
                    uv.x = diff.x;
                    uv.y = diff.z;
                    break;
                case Gridside.zy:
                    uv.x = diff.z;
                    uv.y = diff.y;
                    break;
            }
            if (selectedUV != null)
                uv += selectedUV.uv;

            return uv;
        }

        public float dragDelay;
        bool isDragging = false;
        public bool draggingSelected { get { return isDragging; } set { isDragging = GodMode.disableRotation = value; } }

        public void DisconnectDragged()
        {
            Debug.Log("Disconnecting dragged");

            vertexpointDta temp = new vertexpointDta(selectedUV.vert.pos);
            _Mesh.vertices.Add(temp);

            selectedUV.AssignToNewVertex(temp);

            _Mesh.Dirty = true;//_Mesh.GenerateMesh(_targetPiece);
        }

        // G FUNCTION FUNCTIONS
        public void QUICK_G_Functions()
        {

            switch (quickMeshFunctionsExtensions.current)
            {
                case quickMeshFunctionForG.DeleteTrianglesFully:
                    if ((Input.GetKey(KeyCode.G)) && (pointedTris != null))
                    {
                        foreach (UVpoint uv in pointedTris.uvpnts)
                        {
                            if ((uv.vert.uv.Count == 1) && (uv.tris.Count == 1))
                                _Mesh.vertices.Remove(uv.vert);
                        }

                        _Mesh.triangles.Remove(pointedTris);
                        pointedTris = null;
                        pointedUV = null;
                        selectedUV = null;
                        pointedLine = null;
                        _Mesh.Dirty = true;
                    }
                    break;
                case quickMeshFunctionForG.Line_Center_Vertex_Add:
                    if ((Input.GetKeyDown(KeyCode.G)) && (pointedLine != null))
                    {
                        Vector3 tmp = pointedLine.pnts[0].vert.pos;
                        tmp += (pointedLine.pnts[1].vert.pos - pointedLine.pnts[0].vert.pos) / 2;
                        _Mesh.insertIntoLine(pointedLine.pnts[0].vert, pointedLine.pnts[1].vert, tmp);

                    }
                    break;
                case quickMeshFunctionForG.TrisColorForBorderDetection:
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        Debug.Log("Pointed Line null: " + (pointedLine == null));

                        if (pointedTris != null)
                        {
                            for (int i = 0; i < 3; i++)
                                pointedTris.uvpnts[i].tmpMark = false;
                            bool[] found = new bool[3];
                            Color[] cols = new Color[3];
                            cols[0] = new Color(0, 1, 1, 1);
                            cols[1] = new Color(1, 0, 1, 1);
                            cols[2] = new Color(1, 1, 0, 1);

                            for (int j = 0; j < 3; j++)
                            {
                                for (int i = 0; i < 3; i++)
                                    if ((!found[j]) && (pointedTris.uvpnts[i]._color == cols[j])) {
                                        pointedTris.uvpnts[i].tmpMark = true;
                                        found[j] = true;
                                    }
                            }

                            for (int j = 0; j < 3; j++)
                            {
                                for (int i = 0; i < 3; i++)
                                    if ((!found[j]) && (!pointedTris.uvpnts[i].tmpMark))
                                    {
                                        pointedTris.uvpnts[i].tmpMark = true;
                                        pointedTris.uvpnts[i]._color = cols[j];
                                        found[j] = true;
                                    }
                            }


                            _Mesh.Dirty = true;
                        }
                        else if (pointedLine != null)
                        {
                            UVpoint a = pointedLine.pnts[0];
                            UVpoint b = pointedLine.pnts[1];
                            UVpoint lessTris = (a.tris.Count < b.tris.Count) ? a : b;

                            if ((a._color.r > 0.9f) && (b._color.r > 0.9f))
                                lessTris._color.r = 0;
                            else if ((a._color.g > 0.9f) && (b._color.g > 0.9f))
                                lessTris._color.g = 0;
                            else if ((a._color.b > 0.9f) && (b._color.b > 0.9f))
                                lessTris._color.b = 0;

                            _Mesh.Dirty = true;

                        }
                    }
                    break;

                case quickMeshFunctionForG.Path:
                   // if (selectedLine != null)
                     //   VertexLine(selectedLine.pnts[0].vert, selectedLine.pnts[1].vert, new Color(0.7f, 0.8f, 0.5f, 1));
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        if (G_toolDta.updated)
                            ExtendPath();
                        else
                            SetPathStart();
                    }




                    break;
                case quickMeshFunctionForG.MakeOutline:
                    if ((Input.GetKeyDown(KeyCode.G)) && (pointedUV != null))
                    {

                        //	_Mesh.RefresVerticleTrisList();
                        List<LineData> AllLines = pointedUV.vert.GetAllLines_USES_Tris_Listing();


                        int linesFound = 0;
                        LineData[] lines = new LineData[2];


                        for (int i = 0; i < AllLines.Count; i++)
                        {
                            if (AllLines[i].trianglesCount == 0)
                            {

                                if (linesFound < 2)
                                    lines[linesFound] = AllLines[i];
                                else return;
                                linesFound++;
                            }
                        }

                        if (linesFound == 2)
                        {
                            Vector3 norm = lines[0].HalfVectorToB(lines[1]);

                            vertexpointDta hold = new vertexpointDta(pointedUV.vert.pos);

                            if (selectedUV != null)
                                new UVpoint(hold, selectedUV.getUV(0), selectedUV.getUV(1));
                            else
                                new UVpoint(hold);

                            _Mesh.vertices.Add(hold);
                            MoveVertexToGrid(hold);
                            hold.pos += norm * outlineWidth;

                            UVpoint[] tri = new UVpoint[3];

                            for (int i = 0; i < 2; i++)
                            {
                                tri[0] = hold.uv[0];
                                tri[1] = lines[i].pnts[1];
                                tri[2] = lines[i].pnts[0];

                                _Mesh.triangles.Add(new trisDta(tri));
                            }

                            _Mesh.Dirty = true;
                        }




                    }

                    break;
            }

        }

        public void SetPathStart()
        {
            if (selectedLine == null) return;

            List<trisDta> td = selectedLine.getAllTriangles_USES_Tris_Listing();

            if (td.Count != 1) return;



            UVpoint third = td[0].NotOnLine(selectedLine);


            var alltris = third.vert.triangles();

            if (alltris.Count == 1)
            {
                Debug.Log("Only one tris in third");
                return;
            }

            float MinDist = -1;
            UVpoint fourth = null;
            trisDta secondTris = null;

            foreach (trisDta tris in alltris)
            {
                if (tris.includes(selectedLine.pnts[0].vert) != tris.includes(selectedLine.pnts[1].vert))
                {
                    UVpoint otherUV = tris.NotOneOf(new UVpoint[] { selectedLine.pnts[0], selectedLine.pnts[1], third });

                    float sumDist;
                    float dist;
                    dist = Vector3.Distance(selectedLine.pnts[0].vert.pos, otherUV.vert.pos);
                    sumDist = dist * dist;
                    dist = Vector3.Distance(otherUV.vert.pos, selectedLine.pnts[1].vert.pos);
                    sumDist += dist * dist;
                    dist = Vector3.Distance(otherUV.vert.pos, third.vert.pos);
                    sumDist += dist * dist;


                    if ((MinDist == -1) || (MinDist > sumDist))
                    {
                        secondTris = tris;
                        fourth = otherUV;
                        MinDist = sumDist;
                    }
                }
            }

            if (secondTris == null)
            {
                Debug.Log("Third tris not discovered");
                return;
            }

            Vector3 frontCenter = (selectedLine.pnts[0].vert.pos + selectedLine.pnts[1].vert.pos) / 2;
            Vector3 backCenter = (third.vert.pos + fourth.vert.pos) / 2;

            G_toolDta.PrevDirection = frontCenter - backCenter;

            float distance = (frontCenter - backCenter).magnitude;

            Vector2 frontCenterUV = (selectedLine.pnts[0].uv + selectedLine.pnts[1].uv) / 2;
            Vector2 backCenterUV = (third.uv + fourth.uv) / 2;

            G_toolDta.uvChangeSpeed = (frontCenterUV - backCenterUV) / distance;
            G_toolDta.width = selectedLine.Vector().magnitude;

            Debug.Log("Path is: " + G_toolDta.width + " wight and " + G_toolDta.uvChangeSpeed + " uv change per square");

            if (Mathf.Abs(G_toolDta.uvChangeSpeed.x) > Mathf.Abs(G_toolDta.uvChangeSpeed.y))
                G_toolDta.uvChangeSpeed.y = 0;
            else
                G_toolDta.uvChangeSpeed.x = 0;

            G_toolDta.updated = true;
        }
        void ExtendPath()
        {
            if (G_toolDta.updated == false) return;
            if (selectedLine == null) { G_toolDta.updated = false; return; }

            UpdateLocalSpaceV3s();

            Vector3 previousCenterPos = selectedLine.pnts[0].vert.pos;

            Vector3 previousAB = selectedLine.pnts[1].vert.pos - selectedLine.pnts[0].vert.pos;

            previousCenterPos += (previousAB / 2);



            Vector3 vector = onGridLocal - previousCenterPos;
            float distance = vector.magnitude;

            vertexpointDta a = new vertexpointDta(selectedLine.pnts[0].vert.pos);
            vertexpointDta b = new vertexpointDta(selectedLine.pnts[1].vert.pos);

            _Mesh.vertices.Add(a);
            _Mesh.vertices.Add(b);

            UVpoint aUV = new UVpoint(a, selectedLine.pnts[0].uv + G_toolDta.uvChangeSpeed * distance);
            UVpoint bUV = new UVpoint(b, selectedLine.pnts[1].uv + G_toolDta.uvChangeSpeed * distance);




            _Mesh.triangles.Add(new trisDta(new UVpoint[] { selectedLine.pnts[0], bUV, selectedLine.pnts[1] }));
            trisDta headTris = new trisDta(new UVpoint[] { selectedLine.pnts[0], aUV, bUV });

            _Mesh.triangles.Add(headTris);

            //  

            switch (G_toolDta.mode)
            {
                case gtoolPathConfig.ToPlanePerpendicular:
                    //vector = previousCenterPos.DistanceV3To(ptdPos);

                    a.pos = onGridLocal;
                    b.pos = onGridLocal;


                    Vector3 cross = Vector3.Cross(vector, GridNavigator.inst().getGridPerpendicularVector()).normalized * G_toolDta.width / 2;
                    a.pos += cross;
                    b.pos += -cross;



                    break;
                case gtoolPathConfig.Rotate:
                    // Vector3 ab = a.pos.DistanceV3To(b.pos).normalized * gtoolPath.width;

                    a.pos = onGridLocal;
                    b.pos = onGridLocal;



                    Quaternion rot = Quaternion.FromToRotation(previousAB, vector);
                    Vector3 rotv3 = (rot * vector).normalized * G_toolDta.width / 2;
                    a.pos += rotv3;
                    b.pos += -rotv3;


                    break;

                case gtoolPathConfig.AsPrevious:
                    a.pos += vector;
                    b.pos += vector;
                    break;
            }

            G_toolDta.PrevDirection = vector;

            selectedLine = new LineData(headTris, aUV, bUV);

            _Mesh.Dirty = true;
        }

        void MoveVertexToGrid(vertexpointDta vp)
        {
            UpdateLocalSpaceV3s();
            Vector3 diff = onGridLocal - vp.pos;

            diff.Scale(GridNavigator.inst().getGridPerpendicularVector());
            vp.pos += diff;
        }
        public void AssignSelected(UVpoint newpnt)
        {
            selectedUV = newpnt;
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                MoveVertexToGrid(selectedUV.vert);
                _Mesh.Dirty = true;
            }
            else
                if (!EditorInputManager.getControlKey())
            {
                GridNavigator.onGridPos = selectedUV.vert.getWorldPos();
                //  Debug.Log("Moving grid pos to " + GridNavigator.onGridPos);
                UpdateLocalSpaceV3s();
                GridNavigator.inst().UpdatePositions();
                // Debug.Log("Result: "+GridNavigator.onGridPos);
            }

            trisVerts = 0;

            if (UVnavigator._inst != null)
                UVnavigator._inst.CenterOnUV(selectedUV.uv);
        }

        public bool DeleteVertHEAL(vertexpointDta vert)
        {

           

            trisDta[] trs = new trisDta[3];

            int cnt = 0;

            for (int i = 0; i < _Mesh.triangles.Count; i++)
            {
                if (_Mesh.triangles[i].includes(vert))
                {
                    if (cnt == 3) return false;
                    trs[cnt] = _Mesh.triangles[i];
                    cnt++;
                }
            }

            if (cnt != 3) return false;


            trs[0].MergeAround(trs[1], vert); //Consume(trs[1]);
            _Mesh.triangles.Remove(trs[1]);
            _Mesh.triangles.Remove(trs[2]);

           // if ((selectedLine != null) && (selectedLine.includes(vert))) selectedLine = null;
            //if ((selectedUV != null) && (selectedUV.vert == vert)) selectedUV = null;

            _Mesh.vertices.Remove(vert);


            trisVerts = 0;

            NullPoinedSelected();

            return true;
        }

        public void SwapLine(vertexpointDta a, vertexpointDta b)
        {
            NullPoinedSelected();

            trisDta[] trs = new trisDta[2];
            int cnt = 0;
            for (int i = 0; i < _Mesh.triangles.Count; i++)
            {
                trisDta tmp = _Mesh.triangles[i];
                if (tmp.includes(a, b))
                {
                    if (cnt == 2) return;
                    trs[cnt] = tmp;
                    cnt++;
                }
            }
            if (cnt != 2) return;

            UVpoint nol0 = trs[0].NotOnLine(a, b);
            UVpoint nol1 = trs[1].NotOnLine(a, b);

            trs[0].Replace(trs[0].GetByVert(a), nol1);
            trs[1].Replace(trs[1].GetByVert(b), nol0);

            trisVerts = 0;
        }

        public void DeleteLine(LineData ld)
        {
            NullPoinedSelected();

            _Mesh.RemoveLine(ld);

            if (isInTrisSet(ld.pnts[0]) || isInTrisSet(ld.pnts[1]))
                trisVerts = 0;

        }

        public void DeleteUv(UVpoint uv)
        {
            vertexpointDta vrt = uv.vert;

            NullPoinedSelected();
            /*  if (pointedUV == uv) pointedUV = null;
              if (selectedUV == uv) selectedUV = null;
              if ((selectedTris != null) && selectedTris.includes(uv.vert)) selectedTris = null;
              if ((selectedLine != null) && (selectedLine.includes(uv))) selectedLine = null;
              if ((pointedTris != null) && pointedTris.includes(uv.vert)) pointedTris = null;
              if ((pointedLine != null) && (pointedLine.includes(uv))) pointedLine = null;
              */

            for (int i = 0; i < _Mesh.triangles.Count; i++)
            {
                if (_Mesh.triangles[i].includes(uv))
                {
                    _Mesh.triangles.RemoveAt(i);
                    i--;
                }
            }

            if (isInTrisSet(uv))
                trisVerts = 0;


            vrt.uv.Remove(uv);


            if (vrt.uv.Count == 0)
            {

                _Mesh.vertices.Remove(vrt);
            }



            _Mesh.Dirty = true;
        }

        public void NullPoinedSelected()
        {
            pointedUV = null;
            pointedLine = null;
            pointedTris = null;
            selectedUV = null;
            selectedLine = null;
            selectedTris = null;
        }

        public bool isInTrisSet(vertexpointDta vert)
        { // Only one Unique coordinate per triangle
            for (int i = 0; i < trisVerts; i++)
                if (TrisSet[i].vert == vert) return true;
            return false;
        }

        public bool isInTrisSet(UVpoint uv)
        { // Only one Unique coordinate per triangle
            for (int i = 0; i < trisVerts; i++)
                if (TrisSet[i] == uv) return true;
            return false;
        }

        public vertexpointDta AddPoint(Vector3 pos)
        {
            vertexpointDta hold = new vertexpointDta(pos);
            // hold.uv.Add(
            new UVpoint(hold);
            _Mesh.vertices.Add(hold);

            // if (m_CapsLock)
            if (!EditorInputManager.getControlKey())
                AddToTrisSet(hold.uv[0]);

            if (cfg.pixelPerfectMeshEditing)
                hold.PixPerfect();

            return hold;
        }

        public void MakeTriangleVertUnique(trisDta tris, UVpoint pnt)
        {
            // bool duplicant = false;

            /* for (int i = 0; i < _Mesh.triangles.Count; i++) {
                 trisDta other = _Mesh.triangles[i];
                 if ((other.includes(pnt)) && (other != tris))   {
                     duplicant = true;
                     break;
                 }
             }*/
            //if (!duplicant) return;

            if (pnt.tris.Count == 1) return;

            UVpoint nuv = new UVpoint(pnt.vert, pnt);

            tris.Replace(pnt, nuv);

            _Mesh.Dirty = true;


        }

        /*  public void insertIntoTriangleUniqueVerticles(trisDta a, Vector3 pos)   {
             //Debug.Log("Inserting into triangle");
             vertexpointDta newVrt = new vertexpointDta(pos);
             _Mesh.vertices.Add(newVrt);

             UVpoint[] newUV = new UVpoint[3]; // (newVrt);
             Vector2 newV2_0 = a.PointUVonTriangle(pos,0);
             Vector2 newV2_1 = a.PointUVonTriangle(pos,1);
             for (int i = 0; i < 3; i++)
             {
                 newUV[i] = new UVpoint(newVrt, newV2_0, newV2_1);

               }

             trisDta b = new trisDta(a.uvpnts);
             trisDta c = new trisDta(a.uvpnts);

             a.uvpnts[0] = newUV[0];
             b.uvpnts[1] = newUV[1];
             c.uvpnts[2] = newUV[2];

             _Mesh.triangles.Add(b);
             _Mesh.triangles.Add(c);

             _Mesh.Dirty = true;//_Mesh.GenerateMesh(_targetPiece);

         }

        public void insertIntoTriangle(trisDta a, Vector3 pos) {
            // Debug.Log("Inserting into triangle");
             vertexpointDta newVrt = new vertexpointDta(pos);
             UVpoint newUV = new UVpoint(newVrt, a.PointUVonTriangle(pos, 0), a.PointUVonTriangle(pos, 0));

             _Mesh.vertices.Add(newVrt);

             trisDta b = new trisDta(a.uvpnts);
             trisDta c = new trisDta(a.uvpnts);

             a.Replace(0, newUV);//uvpnts[0] = newUV;
             b.Replace(1, newUV);// uvpnts[1] = newUV;
             c.Replace(2, newUV);// uvpnts[2] = newUV;

             _Mesh.triangles.Add(b);
             _Mesh.triangles.Add(c);

             _Mesh.Dirty = true;// _Mesh.GenerateMesh(_targetPiece);

         }*/

        bool ProcessLinesOnTriangle(trisDta t)
        {
            t.wasProcessed = true;
            const float percision = 0.025f;

            if (MyMath.isPointOnLine(t.uvpnts[0].vert.distanceToPointed, t.uvpnts[1].vert.distanceToPointed, Vector3.Distance(t.uvpnts[0].vert.pos, t.uvpnts[1].vert.pos), percision))
            {
                ProcessPointOnALine(t.uvpnts[0], t.uvpnts[1], t);
                return true;
            }

            if (MyMath.isPointOnLine(t.uvpnts[1].vert.distanceToPointed, t.uvpnts[2].vert.distanceToPointed, Vector3.Distance(t.uvpnts[1].vert.pos, t.uvpnts[2].vert.pos), percision))
            {
                ProcessPointOnALine(t.uvpnts[1], t.uvpnts[2], t);
                return true;
            }

            if (MyMath.isPointOnLine(t.uvpnts[2].vert.distanceToPointed, t.uvpnts[0].vert.distanceToPointed, Vector3.Distance(t.uvpnts[2].vert.pos, t.uvpnts[0].vert.pos), percision))
            {
                ProcessPointOnALine(t.uvpnts[2], t.uvpnts[0], t);
                return true;
            }


            return false;
        }

        void GetPointedTRIANGLESorLINE()
        {

            _Mesh.tagTrianglesUnprocessed();

            UpdateLocalSpaceV3s();

            for (int i = 0; i < _Mesh.vertices.Count; i++)
                foreach (UVpoint uv in _Mesh.vertices[i].uv)
                    foreach (trisDta t in uv.tris)
                        if (!t.wasProcessed)
                        {
                            //	Debug.Log ("Browsing");
                            t.wasProcessed = true;
                            if (t.PointOnTriangle())
                            {

                                if (EditorInputManager.GetMouseButtonDown(1))
                                {
                                    selectedTris = t;
                                    AssignSelected(t.GetClosestTo(collisionPosLocal));
                                }

                                pointedTris = t;

                                if (tool.showLines)
                                    ProcessLinesOnTriangle(pointedTris);

                                return;
                            }

                        }


        }
        void ManageDragging()
        {
            if ((EditorInputManager.GetMouseButtonUp(1)) || (EditorInputManager.GetMouseButton(1) == false))
            {
                draggingSelected = false;
                _Mesh.Dirty = true;

            }
            else
            {
                dragDelay -= Time.deltaTime;
                if ((dragDelay < 0) || (Application.isPlaying == false))
                {
                    if (selectedUV == null) { draggingSelected = false; Debug.Log("no selected"); return; }
                    if ((GridNavigator.inst().angGridToCamera(GridNavigator.onGridPos) < 82))
                    {
                        switch (_meshTool)
                        {
                            case MeshTool.vertices:
                                selectedUV.vert.pos = onGridLocal;
                                break;
                            case MeshTool.VertexAnimation:
                                if (target.AnimatedVertices())
                                {
                                    selectedUV.vert.AnimateTo(onGridLocal);
                                }
                                break;
                        }
                    }
                }
            }
        }
        bool ManageRaycast()
        {
            RaycastHit hit;
            pointedUV = null;
            bool VertexIsPointed = false;
            if (_Mesh.vertices.Count > 0)
            {
                if ((!EditorInputManager.getAltKey()) && Physics.Raycast(EditorInputManager.GetScreenRay(), out hit))
                {

                    VertexIsPointed = (hit.transform.tag == "VertexEd");
                  
                    if (VertexIsPointed) {
                        GridNavigator.collisionPos = hit.transform.position;
                        UpdateLocalSpaceV3s();
                        _Mesh.SortAround(collisionPosLocal, true);

                    } else  {
                        GridNavigator.collisionPos = hit.point;
                        UpdateLocalSpaceV3s();
                        _Mesh.SortAround(collisionPosLocal, true);
                        GetPointedTRIANGLESorLINE();
                    } 
  
                } else 
                    GridNavigator.collisionPos = GridNavigator.onGridPos;
                UpdateLocalSpaceV3s();
            }
            return VertexIsPointed;
        }

        void ManagePointedUV(vertexpointDta pointedVX)
        {
           
        
            if (currentUV == pointedVX.uv.Count) currentUV--;

            if ((selectedUV != null) && (selectedUV.vert == pointedVX) && (!SelectingUVbyNumber))
                pointedUV = selectedUV;
            else
                pointedUV = pointedVX.uv[currentUV];
        }

        /* void ManageToolVertColorOnLine(UVpoint a, UVpoint b, trisDta t) {
             if (_meshTool != MeshTool.VertColor) return;

            // LineData ld = new LineData(t, a, b);

                 if (Input.GetMouseButtonDown(1))
                 {
                     a.vert.RemoveBorderFromLine(b.vert);
                     _Mesh.Dirty = true;
                 }

              if (EditorInputManager.GetMouseButtonDown(0))  {
                     BrushConfig bcf = cfg.brushConfig;

                     a.vert.SetColorOnLine(bcf.color.ToColor(), bcf.mask, b.vert);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
                     b.vert.SetColorOnLine(bcf.color.ToColor(), bcf.mask, a.vert);
                     _Mesh.Dirty = true;
                 }

                 if (Input.GetKeyDown(KeyCode.Alpha1))  {
                 a.vert.FlipChanelOnLine(ColorChanel.R, b.vert);
                 _Mesh.Dirty = true;
                 }

                 if (Input.GetKeyDown(KeyCode.Alpha2))
                 {
                     a.vert.FlipChanelOnLine(ColorChanel.G, b.vert);
                     _Mesh.Dirty = true;
                 }

                 if (Input.GetKeyDown(KeyCode.Alpha3))
                 {
                     a.vert.FlipChanelOnLine(ColorChanel.B, b.vert);
                     _Mesh.Dirty = true;
                 }

                 if (Input.GetKeyDown(KeyCode.Alpha4))
                 {
                     a.vert.FlipChanelOnLine(ColorChanel.A, b.vert);
                     _Mesh.Dirty = true;
                 }

         }*/

        void ProcessPointOnALine(UVpoint a, UVpoint b, trisDta t)
        {

            if (EditorInputManager.GetMouseButtonDown(1))
            {
                selectedLine = new LineData(t, a, b);
                UpdateLocalSpaceV3s();

                G_toolDta.updated = false;

                if (quickMeshFunctionForG.Path.selected())
                    SetPathStart();

            }

            pointedLine = new LineData(t, new UVpoint[] { a, b });

        

        }

        void PROCESS_KEYS()
        {

            MeshToolBase t = tool;
            
            if ((t.showVertices) && (pointedUV != null))
                t.KeysEventPointedVertex();
            else if ((t.showLines) && (pointedLine != null))
                t.KeysEventPointedLine();
            else if ((t.showTriangles) && (pointedTris != null))
                t.KeysEventPointedTriangle();
        }

        void RAYCAST_SELECT_MOUSEedit()
        {

            pointedTris = null;
            pointedLine = null;

            UpdateLocalSpaceV3s();

            if (draggingSelected)
                ManageDragging();
            else  {

                if ((ManageRaycast()) && (currentUV <= _Mesh.vertices[0].uv.Count))   {
                    ManagePointedUV(_Mesh.vertices[0]);
                    if (EditorInputManager.GetMouseButtonDown(1))
                        AssignSelected(pointedUV);
                }

                MeshToolBase t = tool;

                if ((t.showVertices) && (pointedUV != null))
                    t.MouseEventPointedVertex();
                else if ((t.showLines) && (pointedLine != null))
                    t.MouseEventPointedLine();
                else if ((t.showTriangles) && (pointedTris != null))
                    t.MouseEventPointedTriangle();
                else t.MouseEventPointedNothing();

                QUICK_G_Functions();

            }
        }

        void SORT_AND_UPDATE_UI() {

        

            if (grid.verts[0].go == null)
                InitVertsIfNUll();

           // if (_meshTool == MeshTool.vertices)
             //   DrowLinesAroundTargetPiece();

            UpdateLocalSpaceV3s();

            _Mesh.SortAround(collisionPosLocal, false);

            float scaling = 16;

            grid.selectedVertex.go.ActiveUpdate(false);
            grid.pointedVertex.go.ActiveUpdate(false);

            for (int i = 0; i < vertsShowMax; i++)
                grid.verts[i].go.ActiveUpdate(false);

            if (tool.showVertices)
            for (int i = 0; i < vertsShowMax; i++)
                if (_Mesh.vertices.Count > i)  {
                    MarkerWithText mrkr = grid.verts[i];
                    vertexpointDta vpoint = _Mesh.vertices[i];

                    Vector3 worldPos = vpoint.getWorldPos();
                    float tmpScale;
                    tmpScale = Vector3.Distance(worldPos,
                        transform.gameObject.tryGetCameraTransform().position) / scaling;
                        
                    if (GetPointedVert() == vpoint)
                    {
                        mrkr = grid.pointedVertex; tmpScale *= 2;
                    }
                    else if (GetSelectedVert() == _Mesh.vertices[i])
                    {
                        mrkr = grid.selectedVertex;
                            tmpScale *= 1.5f;
                    }

                    mrkr.go.ActiveUpdate(true);
                    mrkr.go.transform.position = worldPos;
                    mrkr.go.transform.rotation = transform.gameObject.tryGetCameraTransform().rotation;
                    mrkr.go.transform.localScale = new Vector3((isInTrisSet(vpoint) ? 1.5f : 1) * tmpScale, tmpScale, tmpScale);

                    Ray tmpRay = new Ray();
                    RaycastHit hit;
                    tmpRay.origin = transform.gameObject.tryGetCameraTransform().position;
                    tmpRay.direction = mrkr.go.transform.position - tmpRay.origin;

                    if ((Physics.Raycast(tmpRay, out hit, cfg.MaxDistanceForTransformPosition)) && (hit.transform.tag != "VertexEd"))
                        mrkr.go.ActiveUpdate(false);

                    if (sameTrisAsPointed(vpoint))
                    {
                        mrkr.textm.color = Color.white;

                    }
                    else
                        mrkr.textm.color = Color.gray;


                    AssignText(mrkr, vpoint);

                }


        }

      

        public void DRAW_Lines(bool isGizmoCall)
        {

            GizmoLines = isGizmoCall;

            if (target == null) return;

			//Gizmos.DrawSphere (_target.transform.InverseTransformPoint(collisionPosLocal), _Mesh.distanceLimit*_target.transform.lossyScale.x);

            if (tool.showTriangles)
            {
                if ((pointedTris != null) && ((pointedTris != selectedTris) || (!tool.showSelectedTriangle)))
                    outlineTriangle(pointedTris, Color.cyan, Color.gray);

                if ((selectedTris != null) && (tool.showSelectedTriangle))
                    outlineTriangle(selectedTris, Color.blue, Color.white);
            }

            if (tool.showLines)
            {
                if (pointedLine != null)
                    Line(pointedLine.pnts[0].vert, pointedLine.pnts[1].vert,
                    (_meshTool != MeshTool.VertColor) ? Color.green : pointedLine.pnts[0]._color * pointedLine.pnts[1]._color);

                for (int i = 0; i < Mathf.Min(vertsShowMax, _Mesh.vertices.Count); i++)
                {
                    vertexpointDta vp = _Mesh.vertices[i];
                    if (sameTrisAsPointed(vp))
                        Line(vp, pointedUV.vert, Color.yellow);
                }
            }

            if (tool.showVertices)
            {

                if (pointedUV != null)
                {
                    for (int i = 0; i < _Mesh.triangles.Count; i++)
                    {
                        trisDta td = _Mesh.triangles[i];
                        if (td.includes(pointedUV))
                        {

                            Line(td.uvpnts[1].vert, td.uvpnts[0].vert, Color.yellow);
                            Line(td.uvpnts[1].vert, td.uvpnts[2].vert, Color.yellow);
                            Line(td.uvpnts[2].vert, td.uvpnts[0].vert, Color.yellow);
                        }
                    }
                    Vector3 selPos = pointedUV.vert.getWorldPos(); //.pos.ToV3 (false);
                    //Gizmos.color = Color.green;
                    //Gizmos.DrawLine(selPos, GridNavigator.inst().ProjectToGrid(selPos));
                    Line(selPos, GridNavigator.inst().ProjectToGrid(selPos), Color.green);
                }

                /*if (selectedUV != null)
                {
                    Vector3 selPos = selectedUV.vert.getWorldPos();//.pos.ToV3 (false);
                    Debug.DrawLine(selPos, GridNavigator.inst().ProjectToGrid(selPos), Color.green);
                }*/
            }
        }

        public void CombinedUpdate()
        {
            
			showGrid = ((target != null) && (target.enabled) && ((_meshTool == MeshTool.vertices) || (_meshTool == MeshTool.VertexAnimation) || ((_meshTool == MeshTool.uv) && GridToUVon)));

            GridNavigator.inst().SetEnabled(showGrid, cfg.SnapToGrid && showGrid);

            if (target == null)
                return;

			if (!target.enabled)
            {
                DisconnectMesh();
                return;
            }



            int no = EditorInputManager.GetNumberKeyDown();
            SelectingUVbyNumber = false;
            if (no != -1) { currentUV = no - 1; SelectingUVbyNumber = true; } else currentUV = 0;


            if (Application.isPlaying)
                UpdateInputPlaytime();

            GridNavigator.inst().UpdatePositions();


            if (Application.isPlaying)
                SORT_AND_UPDATE_UI();

         

            if (_Mesh.Dirty)
                Redraw();

        }

        void updateGrid()
        {
            GridNavigator.inst().UpdatePositions();
            UpdateLocalSpaceV3s();
        }

#if UNITY_EDITOR
        public void UpdateInputEditorTime(Event e, Ray ray, bool up, bool dwn)
        {

            if (target == null)
                return;

            EditorInputManager.raySceneView = ray;

            if (e.type == EventType.keyDown) {
                switch (e.keyCode)
                {

                    case KeyCode.Delete: //Debug.Log("Use Backspace to delete vertices"); goto case KeyCode.Backspace;
                    case KeyCode.Backspace:
                        if (pointedUV != null) DeleteUv(pointedUV); else if (selectedUV != null) DeleteUv(selectedUV); e.Use(); break;
                }
                PROCESS_KEYS();
            }

            if (e.type == EventType.ScrollWheel)
            {
                if (GridNavigator.inst().ScrollsProcess(e.delta.y))
                    ProcessScaleChange();
                updateGrid();

                e.Use();
            }

            if (e.isMouse || (e.type == EventType.ScrollWheel)) {

                EditorInputManager.feedMouseEvent(e);
                updateGrid();

                RAYCAST_SELECT_MOUSEedit();
            }

            SORT_AND_UPDATE_UI();

            return;
        }
#endif

        public void UpdateInputPlaytime()
        {

            if (pegi.mouseOverUI)
                return;
              //  return;

            if (GridNavigator.inst().ScrollsProcess(Input.GetAxis("Mouse ScrollWheel")))
                ProcessScaleChange();

            updateGrid();
            RAYCAST_SELECT_MOUSEedit();
            PROCESS_KEYS();

            if (Input.GetMouseButton(2))
                UnityHelperFunctions.SpinAround(GridNavigator.collisionPos, transform.gameObject.tryGetCameraTransform());
        }

        // Not redirected yet
        public void editingUpdate()
        {
            if ((Application.isPlaying == false)) // && (_target != null ) && (UnityHelperFunctions.getFocused() == _target))
                CombinedUpdate();
        }

        public void Update()
        {
            if (Application.isPlaying)
                CombinedUpdate();
        }

        public vertexpointDta GetPointedVert()
        {
            if (pointedUV != null) return pointedUV.vert;
            return null;
        }
        public vertexpointDta GetSelectedVert()
        {
            if (selectedUV != null) return selectedUV.vert;
            return null;
        }
        bool sameTrisAsPointed(vertexpointDta uvi)
        {
            if (pointedUV == null) return false;
            foreach (trisDta t in _Mesh.triangles)
            {
                if (t.includes(uvi) && t.includes(pointedUV)) return true;
            }
            return false;
        }

        void AssignText(MarkerWithText mrkr, vertexpointDta vpoint)
        {

            if (_meshTool == MeshTool.VertexAnimation)
            {
                mrkr.textm.text = vpoint.index.ToString();
                return;
            }

            if ((vpoint.uv.Count > 1) || (GetSelectedVert() == vpoint))
            {

                Texture tex = target.meshRenderer.sharedMaterial.mainTexture;

                if (GetSelectedVert() == vpoint)
                {
                    mrkr.textm.text = (vpoint.uv.Count > 1) ? ((vpoint.uv.IndexOf(selectedUV) + 1).ToString() + "/" + vpoint.uv.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "")) : "";
                    float tsize = tex == null ? 128 : tex.width;
                    if (_meshTool == MeshTool.uv) mrkr.textm.text +=
                        ("uv: " + (selectedUV.uv.x * tsize) + "," + (selectedUV.uv.y * tsize));
                }
                else
                    mrkr.textm.text = vpoint.uv.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "");
            }
            else mrkr.textm.text = "";
        }

        void outlineTriangle(trisDta t, Color colA, Color colB)
        {
            //bool vrt = tool == VertexPositionTool.inst;
            Line(t.uvpnts[0], t.uvpnts[1], t.ForceSmoothedNorm[0] ? colA : colB, t.ForceSmoothedNorm[1] ? colA : colB);
            Line(t.uvpnts[1], t.uvpnts[2], t.ForceSmoothedNorm[1] ? colA : colB, t.ForceSmoothedNorm[2] ? colA : colB);
            Line(t.uvpnts[0], t.uvpnts[2], t.ForceSmoothedNorm[0] ? colA : colB, t.ForceSmoothedNorm[2] ? colA : colB);
        }


        void Line(UVpoint  a, UVpoint b, Color col, Color colb) {
            Line(a.vert, b.vert, col, colb);
        }

        void Line(vertexpointDta a, vertexpointDta b, Color col, Color colb) {

            Vector3 v3a = a.getWorldPos();
            Vector3 v3b = b.getWorldPos();
            Vector3 diff = (v3b - v3a) / 2;
            Line(v3a, v3a + diff, col);
            Line(v3b, v3b - diff, colb);
        }

        void Line(vertexpointDta a, vertexpointDta b, Color col) {
          
            Line(a.getWorldPos(), b.getWorldPos(), col);
        }


        public bool GizmoLines = false;
        void Line(Vector3 from, Vector3 to, Color col) {
            if (GizmoLines) {
                Gizmos.color = col;
                Gizmos.DrawLine(from, to);

            } else 
                Debug.DrawLine(from, to, col);
        }

        public void DrowLinesAroundTargetPiece()
        {

            Vector3 piecePos = target.transform.TransformPoint(-Vector3.one / 2);//PositionScripts.PosUpdate(_target.getpos(), false);


            Vector3 projected = GridNavigator.inst().ProjectToGrid(piecePos); // piecePos * getGridMaskVector() + ptdPos.ToV3(false)*getGridPerpendicularVector();
            Vector3 GridMask = GridNavigator.inst().getGridMaskVector() * 128 + projected;



            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(GridMask.x, projected.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, GridMask.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, projected.y, GridMask.z), Color.red);

            Debug.DrawLine(new Vector3(projected.x, GridMask.y, GridMask.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);
            Debug.DrawLine(new Vector3(GridMask.x, projected.y, GridMask.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);
            Debug.DrawLine(new Vector3(GridMask.x, GridMask.y, projected.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);

            MyDebugClasses.DrawTransformedCubeDebug(target.transform, Color.blue);


        }


       

        void InitVertsIfNUll()
        {
            if (grid.vertPrefab == null)
                grid.vertPrefab = Resources.Load("prefabs/vertex") as GameObject;

            if ((grid.verts == null) || (grid.verts.Length == 0) || (grid.verts[0].go == null))
            {
                grid.verts = new MarkerWithText[vertsShowMax];

                for (int i = 0; i < vertsShowMax; i++)
                {
                    MarkerWithText v = new MarkerWithText();
                    grid.verts[i] = v;
                    v.go = GameObject.Instantiate(grid.vertPrefab);
                    v.go.transform.parent = grid.transform;
                    v.init();
                }
            }

            grid.pointedVertex.init();
            grid.selectedVertex.init();

#if UNITY_EDITOR
            EditorApplication.update -= editingUpdate;
            if (!PainterManager.inst.ApplicationIsAboutToEnterPlayMode())
                EditorApplication.update += editingUpdate;
#endif
        }

        public void OnEnable()
        {

       
            InitVertsIfNUll();

            if ((previouslyEdited != null) && (target == null)) {
                DisconnectMesh();
                EditMesh(previouslyEdited, false);
               
            }

            previouslyEdited = null;

            trisVerts = 0;

        }


        List<PlaytimePainter> selectedPainters = new List<PlaytimePainter>();
        bool showReferences = false;
        bool showTooltip;
        public bool PEGI()
        {
            bool changed = false;
            pegi.newLine();
            pegi.Space();

            pegi.newLine();

            changed |= target.PreviewShaderToggle_PEGI();

            if ((target.originalShader != null) && ("preview".select(45, ref meshSHaderMode.selected, meshSHaderMode.allModes)))
                meshSHaderMode.selected.Apply();

            pegi.newLine();

            pegi.Space();

            pegi.newLine();

            pegi.write("Tool:", 40);

            int before = (int)MeshManager._meshTool;
            cfg._meshTool = (MeshTool)pegi.editEnum(cfg._meshTool);
            if ((before != (int)MeshManager._meshTool) && (target != null))
                grid.UpdateVertColor();

            pegi.newLine();

            pegi.Space();

            pegi.newLine();

            "Mesh Name:".edit(70, ref target.meshNameHolder);
                //target.meshFilter.sharedMesh.name = _Mesh.meshName;

#if UNITY_EDITOR
            if (((AssetDatabase.GetAssetPath(target.getMesh()).Length==0) || (String.Compare(target.meshNameHolder, target.getMesh().name)!=0))  && 
                (icon.save.Click("Save Mesh As "+target.GenerateMeshSavePath(),25).nl())) target.SaveMesh();
#endif

            pegi.newLine();

            if (!selectedPainters.Contains(target))
            {
                if ("Copy Mesh".Click("Add Mesh to the list of meshes to be merged").nl())
                    selectedPainters.Add(target);

                if (selectedPainters.Count > 0)
                {
                    "Will Merge with the following:".nl();
                    for (int i = 0; i < selectedPainters.Count; i++)
                    {
                        if (selectedPainters[i] == null)
                        {
                            selectedPainters.RemoveAt(i);
                            i--;
                        }
                        else
                            selectedPainters[i].gameObject.name.nl();
                    }

                    if ("Merge!".Click().nl()) {

                        foreach (var p in selectedPainters)
                            _Mesh.MergeWith(p);

                        _Mesh.Dirty = true;

                    }
                }

            } else {
                if ("Remove from Copy Selection".Click().nl())
                    selectedPainters.Remove(target);
            }
                


            //pegi.write("Function for G Button:");
            //quickMeshFunctionsExtensions.current = (quickMeshFunctionForG)pegi.editEnum(quickMeshFunctionsExtensions.current);
            pegi.newLine();

            //if (quickMeshFunctionForG.MakeOutline.selected())
              //  "Width".edit(ref outlineWidth).nl();


            //if (!quickMeshFunctionForG.Nothing.selected())
              //  (G_toolDta.toolsHints[(int)quickMeshFunctionsExtensions.current]).nl();

            pegi.newLine();

         

            if (quickMeshFunctionForG.Path.selected()) {

                if (selectedLine == null) "Select Line".nl();
                else {

                    if ("Set path start on selected".Click())
                        SetPathStart();

                    if (G_toolDta.updated == false)
                        "Select must be a Quad with shared uvs".nl();
                    else {
                        if (selectedLine == null)
                            G_toolDta.updated = false;
                        else  {

                            "Mode".write();
                            G_toolDta.mode = (gtoolPathConfig)pegi.editEnum(G_toolDta.mode);
                            "G to extend".nl();


                        }
                    }
                }
            }

            if ("Hint".foldout(ref showTooltip).nl())
                pegi.writeHint(cfg._meshTool.Get().tooltip);


            pegi.nl();

            cfg._meshTool.Get().tool_pegi();

            grid.UpdateVertColor();

            switch (cfg._meshTool)
            {
                case MeshTool.vertices:
                    /* EditorGUILayout.LabelField(" Alt+LMB     - Add vert on grid .");
                     EditorGUILayout.LabelField(" Alt+R_MB    - Select, move vert to grid.");
                     EditorGUILayout.LabelField(" Ctrl+R_MB   - Select, don't change grid.");
                     EditorGUILayout.LabelField(" Ctrl+LMB (on tris)    - Break triangle in 3 with 3 unique UVs");
                     EditorGUILayout.LabelField(" Ctrl+LMB (on grid)    - Add vert, don't connect");
                     EditorGUILayout.LabelField(" Ctrl+Delete - Delete vert, heal triangle");
                     EditorGUILayout.LabelField(" N - Make verticles share normal");*/
                    break;
                case MeshTool.VertColor:
                    " 1234 on Line - apply RGBA for Border.".nl();
                    break;
                case MeshTool.AtlasTexture:
                    "Select Texture and click on triangles".nl();
                    break;
            }

 
            if ((!Application.isPlaying) && ("references".foldout(ref showReferences).nl()))  {

                "vertexPointMaterial".write(grid.vertexPointMaterial);
                pegi.newLine();

                "vertexPrefab".edit(ref grid.vertPrefab).nl();
                "Max Vert Markers ".edit(ref vertsShowMax).nl();
                "pointedVertex".edit(ref grid.pointedVertex.go).nl();
                "SelectedVertex".edit(ref grid.selectedVertex.go).nl();

            }

            return changed;
        }

    }
}