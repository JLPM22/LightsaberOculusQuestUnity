using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows grabbing and throwing of objects with the Grabbable component on them.
/// </summary>
namespace OculusModules.Grab
{
    [RequireComponent(typeof(Rigidbody))]
    public class Grabber : MonoBehaviour
    {
        // Grip trigger thresholds for picking up objects, with some hysteresis.
        public float GrabBeginTh = 0.55f;
        public float GrabEndTh = 0.35f;
        public bool HideOnGrab = true;
        public string GrabberLayer;
        public string GrabbableLayer;
        public OVRInput.Controller Controller;
        // Child/attached transforms of the grabber, indicating where to snap held objects to (if you snap them).
        // Also used for ranking grab targets in case of multiple candidates.
        public Transform GrabPoint;
        public Collider[] GrabVolumes; // Child/attached Colliders to detect candidate grabbable objects.

        private Renderer[] Renderers;
        private int GrabbableLayerInt;

        private float PreviousFlex;
        private Grabbable Grabbable = null;
        private Dictionary<Grabbable, int> GrabCandidates = new Dictionary<Grabbable, int>();
        private Vector3 GrabObjectPosOffset;
        private Quaternion GrabObjectRotOffset;
        private bool GrabVolumeEnabled = true;

        /// <summary>
        /// The currently grabbed object.
        /// </summary>
        public Grabbable GrabbedObject
        {
            get { return Grabbable; }
        }

        public void ForceRelease(Grabbable grabbable)
        {
            bool canRelease = (
                (Grabbable != null) &&
                (Grabbable == grabbable)
            );
            if (canRelease)
            {
                GrabEnd();
            }
        }

        private void Awake()
        {
            Renderers = GetComponentsInChildren<Renderer>();

            int grabberLayer = LayerMask.NameToLayer(GrabberLayer);
            GrabbableLayerInt = LayerMask.NameToLayer(GrabbableLayer);

            foreach (Transform t in GetComponentsInChildren<Transform>()) t.gameObject.layer = grabberLayer;

            OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
            if (rig != null)
            {
                rig.UpdatedAnchors += (r) => { OnUpdateAnchors(); };
            }
        }

        // Hands follow the touch anchors by calling MovePosition each frame to reach the anchor.
        // This is done instead of parenting to achieve workable physics.
        private void OnUpdateAnchors()
        {
            Vector3 destPos = GrabPoint.transform.position;
            Quaternion destRot = GrabPoint.transform.rotation;

            MoveGrabbedObject(destPos, destRot);

            float prevFlex = PreviousFlex;
            PreviousFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller);

            CheckForGrabOrRelease(prevFlex);
        }

        void OnDestroy()
        {
            if (Grabbable != null)
            {
                GrabEnd();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Get the grab trigger
            Grabbable grabbable = other.GetComponentInParent<Grabbable>();
            if (grabbable == null) return;

            // Add the grabbable
            int refCount = 0;
            GrabCandidates.TryGetValue(grabbable, out refCount);
            GrabCandidates[grabbable] = refCount + 1;
        }

        private void OnTriggerExit(Collider other)
        {
            Grabbable grabbable = other.GetComponentInParent<Grabbable>();
            if (grabbable == null) return;

            // Remove the grabbable
            int refCount = 0;
            bool found = GrabCandidates.TryGetValue(grabbable, out refCount);
            if (!found)
            {
                return;
            }

            if (refCount > 1)
            {
                GrabCandidates[grabbable] = refCount - 1;
            }
            else
            {
                GrabCandidates.Remove(grabbable);
            }
        }

        private void CheckForGrabOrRelease(float prevFlex)
        {
            if ((PreviousFlex >= GrabBeginTh) && (prevFlex < GrabBeginTh))
            {
                GrabBegin();
            }
            else if ((PreviousFlex <= GrabEndTh) && (prevFlex > GrabEndTh))
            {
                GrabEnd();
            }
        }

        private void GrabBegin()
        {
            float closestMagSq = float.MaxValue;
            Grabbable closestGrabbable = null;
            Collider closestGrabbableCollider = null;
            Vector3 closestGrabbableRotation = Vector3.zero;

            foreach (Grabbable grabbable in GrabCandidates.Keys)
            {
                for (int j = 0; j < grabbable.GrabPoints.Length; ++j)
                {
                    Collider grabbableCollider = grabbable.GrabPoints[j];
                    // Store the closest grabbable
                    Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(GrabPoint.position);
                    float grabbableMagSq = (GrabPoint.position - closestPointOnBounds).sqrMagnitude;
                    if (grabbableMagSq < closestMagSq)
                    {
                        closestMagSq = grabbableMagSq;
                        closestGrabbable = grabbable;
                        closestGrabbableCollider = grabbableCollider;
                        closestGrabbableRotation = grabbable.GrabRotations[j];
                    }
                }
            }

            // Disable grab volumes to prevent overlaps
            GrabVolumeEnable(false);

            if (closestGrabbable != null)
            {
                if (closestGrabbable.IsGrabbed)
                {
                    closestGrabbable.GrabbedBy.OffhandGrabbed(closestGrabbable);
                }

                Grabbable = closestGrabbable;
                Grabbable.SetLayer(GrabbableLayerInt);
                Grabbable.GrabBegin(this, closestGrabbableCollider, closestGrabbableRotation);

                // Set up offsets for grabbed object desired position relative to hand.
                GrabObjectPosOffset = -Grabbable.GrabbedCollider.transform.localPosition;
                GrabObjectRotOffset = Quaternion.Euler(Grabbable.GrabbedRotation);

                // TODO: force teleport on grab, to avoid high-speed travel to dest which hits a lot of other objects at high
                // speed and sends them flying. The grabbed object may still teleport inside of other objects.
                MoveGrabbedObject(GrabPoint.transform.position, GrabPoint.transform.rotation);

                RenderersEnable(false);
            }
        }

        private void MoveGrabbedObject(Vector3 pos, Quaternion rot)
        {
            if (Grabbable == null)
            {
                return;
            }

            //Rigidbody grabbedRigidbody = Grabbable.Rigidbody;
            Vector3 grabbablePosition = pos + rot * GrabObjectPosOffset;
            Quaternion grabbableRotation = rot; // TODO GrabObjectRotOffset not used... Auto Hand VR...

            Grabbable.transform.position = grabbablePosition;
            Grabbable.transform.rotation = grabbableRotation;
        }

        private void GrabEnd()
        {
            if (Grabbable != null)
            {
                OVRPose localPose = new OVRPose { position = OVRInput.GetLocalControllerPosition(Controller), orientation = OVRInput.GetLocalControllerRotation(Controller) };
                OVRPose offsetPose = new OVRPose { position = transform.localPosition, orientation = transform.localRotation };
                localPose = localPose * offsetPose;

                OVRPose trackingSpace = transform.ToOVRPose() * localPose.Inverse();
                Vector3 linearVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerVelocity(Controller);
                Vector3 angularVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerAngularVelocity(Controller);

                GrabbableRelease(linearVelocity, angularVelocity);
            }

            // Re-enable grab volumes to allow overlap events
            GrabVolumeEnable(true);

            RenderersEnable(true);
        }

        private void GrabbableRelease(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            Grabbable.SetLayer(Grabbable.DefaultLayer);
            Grabbable.GrabEnd(linearVelocity, angularVelocity);
            Grabbable = null;
        }

        private void GrabVolumeEnable(bool enabled)
        {
            if (GrabVolumeEnabled == enabled)
            {
                return;
            }

            GrabVolumeEnabled = enabled;
            for (int i = 0; i < GrabVolumes.Length; ++i)
            {
                Collider grabVolume = GrabVolumes[i];
                grabVolume.enabled = GrabVolumeEnabled;
            }

            if (!GrabVolumeEnabled)
            {
                GrabCandidates.Clear();
            }
        }

        private void OffhandGrabbed(Grabbable grabbable)
        {
            if (Grabbable == grabbable)
            {
                GrabbableRelease(Vector3.zero, Vector3.zero);
            }
        }

        private void RenderersEnable(bool enable)
        {
            for (int i = 0; i < Renderers.Length; ++i)
            {
                if (Renderers[i].enabled == !enable) Renderers[i].enabled = enable;
            }
        }
    }
}