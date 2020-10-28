using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class LightsaberTrail : MonoBehaviour
{
    public float Height = 2.0f;
    public float MinDistance = 0.1f;
    public float TimeTransitionSpeed = 1f;
    public float StartTime = 10.0f;
    public float DesiredTime = 2.0f;
    public float VelocityThreshold = 0.0f;
    public float EndVelocityThreshold = 0.0f;

    private Vector3 LastPosition;
    private Vector3 LastPositionVelocity;
    private float Time;
    private TrailSection LastTrailSection;
    private bool LastTrailUsed = false;
    private bool OneTrailMore = false;
    private float InternalVelocityThreshold = 0.0f;

    private Mesh Mesh;
    private Vector3[] Vertices;

    private MeshRenderer MeshRenderer;
    private Material TrailMaterial;

    private Queue<TrailSection> Sections = new Queue<TrailSection>();

    void Awake()
    {
        MeshFilter meshF = GetComponent<MeshFilter>();
        Mesh = meshF.mesh;
        MeshRenderer = GetComponent<MeshRenderer>();
        TrailMaterial = MeshRenderer.material;
    }

    private void Update()
    {
        UpdateTrail();
        UpdateMesh();
        UpdateTime();

        LastPositionVelocity = transform.position;
    }

    public void UpdateTrail()
    {
        Vector3 position = transform.position;
        float now = UnityEngine.Time.time;

        if ((position - LastPositionVelocity).sqrMagnitude < InternalVelocityThreshold)
        {
            Time = StartTime;
            LastTrailSection = new TrailSection
            {
                Position = position,
                Up = transform.TransformDirection(Vector3.up),
                Time = now
            };
            LastTrailUsed = false;

            if (OneTrailMore && (LastPosition - position).sqrMagnitude > MinDistance * MinDistance)
            {
                // New Section
                TrailSection section = new TrailSection
                {
                    Position = position,
                    Up = transform.TransformDirection(Vector3.up),
                    Time = now
                };

                Sections.Enqueue(section);
                LastPosition = section.Position;
                OneTrailMore = false;
            }
            if (!OneTrailMore)
            {
                InternalVelocityThreshold = VelocityThreshold;
            }

            return;
        }

        if (Sections.Count == 0 || (LastPosition - position).sqrMagnitude > MinDistance * MinDistance)
        {
            InternalVelocityThreshold = EndVelocityThreshold;
            if (!LastTrailUsed)
                Sections.Enqueue(LastTrailSection);
            LastTrailUsed = true;
            OneTrailMore = true;

            // New Section
            TrailSection section = new TrailSection
            {
                Position = position,
                Up = transform.TransformDirection(Vector3.up),
                Time = now
            };

            Sections.Enqueue(section);
            LastPosition = section.Position;
        }
    }

    public void UpdateMesh()
    {
        float time = UnityEngine.Time.time;

        Mesh.Clear();

        // Remove old sections
        while (Sections.Count > 0 && time > Sections.Peek().Time + Time)
        {
            Sections.Dequeue();
        }

        // At least 2 sections to create the line
        if (Sections.Count < 2)
            return;

        Vertices = new Vector3[Sections.Count * 2];

        Matrix4x4 localSpaceTransform = transform.worldToLocalMatrix;

        // Generate vertex
        int i = 0;
        foreach (TrailSection currentSection in Sections)
        {
            // Calculate upwards direction
            Vector3 upDir = currentSection.Up;

            // Generate vertices
            Vertices[i * 2 + 0] = localSpaceTransform.MultiplyPoint(currentSection.Position);
            Vertices[i * 2 + 1] = localSpaceTransform.MultiplyPoint(currentSection.Position + upDir * Height);

            ++i;
        }

        // Generate triangles
        int[] triangles = new int[(Sections.Count - 1) * 2 * 3];
        for (int t = 0; t < triangles.Length / 6; t++)
        {
            triangles[t * 6 + 0] = t * 2;
            triangles[t * 6 + 1] = t * 2 + 1;
            triangles[t * 6 + 2] = t * 2 + 2;

            triangles[t * 6 + 3] = t * 2 + 2;
            triangles[t * 6 + 4] = t * 2 + 1;
            triangles[t * 6 + 5] = t * 2 + 3;
        }

        // Assign to mesh	
        Mesh.vertices = Vertices;
        Mesh.triangles = triangles;
    }

    private void UpdateTime()
    {
        if (Time > DesiredTime)
        {
            Time -= UnityEngine.Time.deltaTime * TimeTransitionSpeed;
            if (Time <= DesiredTime) Time = DesiredTime;
        }
        else if (Time < DesiredTime)
        {
            Time += UnityEngine.Time.deltaTime * TimeTransitionSpeed;
            if (Time >= DesiredTime) Time = DesiredTime;
        }
    }

    public void ClearTrail()
    {
        DesiredTime = 0;
        Time = 0;
        if (Mesh != null)
        {
            Mesh.Clear();
            Sections.Clear();
        }
    }

    public struct TrailSection
    {
        public Vector3 Position;
        public Vector3 Up;
        public float Time;

        public TrailSection(Vector3 position, float time)
        {
            Position = position;
            Up = Vector3.up;
            Time = time;
        }
    }
}