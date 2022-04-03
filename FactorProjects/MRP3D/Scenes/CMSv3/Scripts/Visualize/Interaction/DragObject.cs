using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class DragObject : MonoBehaviour
    {
	    private Vector3 screenPoint;
    	private Vector3 offset;
			
    	void OnMouseDown(){
    		screenPoint = Camera.main.WorldToScreenPoint(transform.position);
    		offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
    	}
			
    	void OnMouseDrag(){
    		Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
    		Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;
    		transform.position = cursorPosition;
		}
    }
}
