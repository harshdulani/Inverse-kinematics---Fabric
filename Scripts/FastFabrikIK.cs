using UnityEditor;
using UnityEngine;

public class FastFabrikIK : MonoBehaviour
{
    [SerializeField] private int chainLength = 2;

    [SerializeField] public Transform target, pole;
	
	[SerializeField] private int iterations = 10;
	[SerializeField] private float delta = 0.01f;
	
    protected Transform[] bones;
    protected Vector3[] positions;
    protected float[] boneLengths;
    protected float totalLength;

	private void Start()
    {
        Init();
    }

	private void LateUpdate()
	{
		ResolveIK();
	}
	
	private void OnDrawGizmos()
	{
		var current = transform;
		for (int i = 0; i < chainLength && current != null && current.parent != null; i++)
		{
			var parentPos = current.parent.position;
			var position = current.position;
			var scale = Vector3.Distance(position, parentPos) * 0.1f;
            
			Handles.matrix = Matrix4x4.TRS(
				position, 
				Quaternion.FromToRotation(Vector3.up, parentPos - position), 
				new Vector3(scale, scale * 10, scale));
			Handles.color = Color.green;
			Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
			current = current.parent;
		}
	}

	private void Init()
    {
        //initialise arrays
        bones = new Transform[chainLength + 1];
        positions = new Vector3[chainLength + 1];
        boneLengths = new float[chainLength + 1];
        totalLength = 0;
        
        //init data
        var current = transform;
        for (var i = bones.Length - 1; i >= 0; i--)
        {
            bones[i] = current;

            if (i == bones.Length - 1)
            {
                //check if this is a leaf bone, leaf bones have no length
            }
            else
			{
				boneLengths[i] = (bones[i + 1].position - current.position).magnitude;
				totalLength += boneLengths[i];
			}
            
            current = current.parent;
        }
    }
	
	private void ResolveIK()
	{
		if(!target) return;
		
		if(bones.Length != chainLength)
			Init();
		
		//get positions
		for (int i = 0; i < bones.Length; i++)
			positions[i] = bones[i].position;
		
		//calculate
		
		//best case, target out of reach
		if ((target.position - bones[0].position).sqrMagnitude > totalLength * totalLength)
		{
			//stretch it along the direction of the target
			var dir = (target.position - positions[0]).normalized;

			//my new position is the position of my parent + my bone length in direction 'dir'
			for (var i = 1; i < positions.Length; i++)
				positions[i] = positions[i - 1] + dir * boneLengths[i - 1];
		}
		else
		{
			for (var iteration = 0; iteration < iterations; iteration++)
			{
				//backwards pass
				for (var i = positions.Length - 1; i > 0; i--)
				{
					if (i == positions.Length - 1)
						positions[i] = target.position;
					else
						positions[i] =
							positions[i + 1] + 
							(positions[i] - positions[i + 1]).normalized * boneLengths[i];
				}
				
				//forward pass
				//maybe cache the final forward pass direction calculations
				for (int i = 1; i < positions.Length - 1; i++)
					positions[i] = 
						positions[i - 1] +
						(positions[i] - positions[i - 1]).normalized * boneLengths[i - 1];
				
				
				//close enough?
				if((positions[positions.Length - 1] - target.position).sqrMagnitude > delta * delta)
					break;
			}
		}
		
		//move towards pole
		if (pole)
		{
			for (int i = 1; i < positions.Length - 1; i++)
			{
				var plane = new Plane(positions[i + 1] - positions[i - 1], positions[i - 1]);
				var projectedPole = plane.ClosestPointOnPlane(pole.position);
				var projectedBone = plane.ClosestPointOnPlane(positions[i]);
				var angle = Vector3.SignedAngle(
					projectedBone - positions[i - 1], 
					projectedPole - positions[i - 1],
					plane.normal);
				positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (positions[i] - positions[i - 1]) +
							   positions[i - 1];
			}
		}
		
		//set positions
		for (int i = 0; i < bones.Length; i++)
			bones[i].position = positions[i];
	}
}
