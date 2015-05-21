using UnityEngine;
using System.Collections;

public class ClosestPointOnMesh : MonoBehaviour {

	public static Vector3 NearestVertexTo(Vector3 point, GameObject gm) { // convert point to local space point = transform.InverseTransformPoint(point);
		
		Mesh mesh = gm.GetComponent<MeshFilter> ().mesh;
		float minDistanceSqr = Mathf.Infinity;
		Vector3 nearestVertex = Vector3.zero;
		
		// scan all vertices to find nearest
		foreach (Vector3 vertex in mesh.vertices)
		{
			Vector3 diff = point-vertex;
			float distSqr = diff.sqrMagnitude;
			
			if (distSqr < minDistanceSqr)
			{
				minDistanceSqr = distSqr;
				nearestVertex = vertex;
			}
		}
		
		// convert nearest vertex back to world space
		return gm.transform.TransformPoint(nearestVertex);
		
	}
}
