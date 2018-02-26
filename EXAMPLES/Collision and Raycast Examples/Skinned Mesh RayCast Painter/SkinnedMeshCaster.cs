﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;



namespace Painter
{

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(SkinnedMeshCaster))]
    public class SkinnedMeshCasterEditor : Editor
    {

        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((SkinnedMeshCaster)target).PEGI().nl();
        }
    }
#endif

    public class SkinnedMeshCaster : MonoBehaviour
    {

        public BrushConfig brush = new BrushConfig();

        void Paint() {

            RaycastHit hit;
            if (Physics.Raycast(new Ray(transform.position, transform.forward), out hit)) {

              var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

              if (painter!= null) {

                    if ((painter.skinnedMeshRendy != null) && (painter.isPaintingInWorldSpace(brush) == false)) {   

                        painter.UpdateColliderForSkinnedMesh();

                        bool colliderDIsabled = !painter.meshCollider.enabled;
                        if (colliderDIsabled) painter.meshCollider.enabled = true;


                        if (!painter.meshCollider.Raycast(new Ray(transform.position, transform.forward), out hit, 99999)) {
                            Debug.Log("Missed the Mesh Collider");
                            if (colliderDIsabled) painter.meshCollider.enabled = false;
                            return;
                        }

                        if (colliderDIsabled) painter.meshCollider.enabled = false;
                    }

                    StrokeVector v = new StrokeVector(hit);
                    
                    painter.SetTexTarget(brush).Paint(v, brush);
                }

            }
        }


#if UNITY_EDITOR
        void OnDrawGizmosSelected() {

            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {

                var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

                Gizmos.color = painter == null ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, hit.point);

            }
            else
            {

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + transform.forward);
            }
        }
#endif


        public bool PEGI()
        {
            bool changed = false;

            if ("Fire!".Click().nl())
                Paint();

            changed |= brush.BrushForTargets_PEGI().nl();
            changed |= brush.Mode_Type_PEGI(brush.TargetIsTex2D).nl();
            changed |= brush.blitMode.PEGI(brush, null);
            Color col = brush.color.ToColor();
            if (pegi.edit(ref col).nl())
                brush.color.From(col);
            changed |= brush.ColorSliders_PEGI();
            return changed;
        }
    }
}