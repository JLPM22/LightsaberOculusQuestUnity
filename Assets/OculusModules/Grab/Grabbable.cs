using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OculusModules.Grab
{
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour
    {
        public Collider[] GrabPoints;
        public Vector3[] GrabRotations;

        [HideInInspector] public Grabber GrabbedBy;
        [HideInInspector] public Collider GrabbedCollider = null;
        [HideInInspector] public Vector3 GrabbedRotation = Vector3.zero;
        [HideInInspector] public int DefaultLayer;
        [HideInInspector] public Rigidbody Rigidbody;

        private bool DefaultKinematic;

        private void Awake()
        {
            DefaultLayer = gameObject.layer;

            if (GrabPoints.Length == 0)
            {
                GrabPoints = new Collider[1]
                {
                    new Collider()
                };
            }
            if (GrabRotations.Length == 0)
            {
                GrabRotations = new Vector3[1]
                {
                    new Vector3(0.0f, 0.0f, 0.0f)
                };
            }

            Rigidbody = GetComponent<Rigidbody>();
            DefaultKinematic = Rigidbody.isKinematic;
        }

        /// <summary>
        /// Notifies the object that it has been grabbed.
        /// </summary>
        public void GrabBegin(Grabber hand, Collider grabPoint, Vector3 grabRot)
        {
            GrabbedBy = hand;
            GrabbedCollider = grabPoint;
            GrabbedRotation = grabRot;
            Rigidbody.isKinematic = true;
        }

        /// <summary>
        /// Notifies the object that it has been released.
        /// </summary>
        virtual public void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            Rigidbody.isKinematic = DefaultKinematic;
            Rigidbody.velocity = linearVelocity; // change this with AddForce??? // TODO
            Rigidbody.angularVelocity = angularVelocity;
            GrabbedBy = null;
            GrabbedCollider = null;
            GrabbedRotation = Vector3.zero;
        }

        public void SetLayer(int layer)
        {
            for (int i = 0; i < GrabPoints.Length; ++i)
            {
                gameObject.layer = layer;
                GrabPoints[i].gameObject.layer = layer;
            }
        }

        /// <summary>
        /// If true, the object is currently grabbed.
        /// </summary>
        public bool IsGrabbed
        {
            get { return GrabbedBy != null; }
        }
    }
}
