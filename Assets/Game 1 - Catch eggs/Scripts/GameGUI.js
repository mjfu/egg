#pragma strict

var messageStyle : GUIStyle;

function Start () {
	messageStyle.fontSize = 26.0;
	messageStyle.fontStyle = FontStyle.Bold;
	messageStyle.normal.textColor = Color.black;
}

function Update () {

}

function OnGUI()
{
	var i : int;
	var bucketAccuracy : float = 0.0;
	var numExits : int;

		
	if (GUI.Button(Rect(Screen.width-150,8.5*Screen.height/10,100,30),"Set Balls")) {
		/* start timer */
		inPlay = true;
	}
	if (GUI.Button(Rect(Screen.width-250,8.5*Screen.height/10,100,30),"Exit Game")) {
		Application.Quit();
	}
	
	GUILayout.BeginArea(Rect((Screen.width/2.0)+250,10,400,100));
		GUILayout.BeginVertical();
        	GUILayout.Label("Total Time: " + elapsedTime, messageStyle);
        	GUILayout.Label("Reptitions: " + boxScript.GetNumReps(), messageStyle);
        	GUILayout.Label("Accuracy: " + parseInt(100*bucketAccuracy) + "%", messageStyle);
        GUILayout.EndVertical();
	GUILayout.EndArea();

	GUILayout.BeginArea(Rect((Screen.width/2.0)-450,10,400,100));
        	GUILayout.Label("Score: " + score, scoreStyle);
        	if (bestIntSec == 0)
        		GUILayout.Label("Best Time:", scoreStyle);
        	else
        		GUILayout.Label("Best Time: " + bestMin + "'" + bestSec + "''", scoreStyle);
        	GUILayout.Label("Timer: " + timerElapsed, scoreStyle);
	GUILayout.EndArea();
	
	GUILayout.BeginArea(Rect(20,8.5*Screen.height/10,170,200));
			GUILayout.BeginVertical();
				GUILayout.BeginHorizontal();
					GUILayout.Label("Hand Open Value");
					maxBendSensorField = GUILayout.TextField(maxBendSensorField);					
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
					GUILayout.Label("Hand Closed Value");
					minBendSensorField = GUILayout.TextField(minBendSensorField);
				GUILayout.EndHorizontal();
			GUILayout.EndVertical();			
	GUILayout.EndArea();

	GUILayout.BeginArea(Rect(200,8.5*Screen.height/10,170,200));
		GUILayout.BeginVertical();
			GUILayout.Label("Bend Sensor Reading = " + boxScript.GetBendSensorVal());
			autoROM = GUILayout.Toggle(autoROM,"Auto Set Range of Motion");
		GUILayout.EndVertical();
	GUILayout.EndArea();
	
	GUILayout.BeginArea(Rect(400,8.5*Screen.height/10,170,200));
		GUILayout.BeginVertical();
			GUILayout.Label("Ball Speed = " + parseInt(ballSpeed*100)/100.0);
			ballSpeed = GUILayout.HorizontalSlider(ballSpeed, 0.5, 3.0);			
			GUILayout.Label("Bucket Size = " + parseInt(bucket.GetComponent(Transform).localScale.x*100)/100.0);
			bucket.GetComponent(Transform).localScale.x = GUILayout.HorizontalSlider(bucket.GetComponent(Transform).localScale.x, 0.5, 5.0);
		GUILayout.EndVertical();
	GUILayout.EndArea();
	
//	if (isLevComplete) {
//		GUI.BeginGroup(Rect(Screen.width/2-150,Screen.height/2-100,350,200));
//			GUI.Box(Rect(0,0,350,200),"Level Complete",messageStyle);
//			if (GUI.Button(Rect(70,70,160,100),"Press to Start Game")) {				
//				count = 3;
//				inPlay = true;
//			}
//		GUI.EndGroup();	
	
	if (GUI.Button(Rect(Screen.width/8.0,Screen.height/2.0,100,30),"Next Level")) {
		//GameManager.setApertures();
		//Application.LoadLevelAdditive((Application.loadedLevel+1)%6);
		inPlay = false;
		bestIntSec = 0;
		for (i=0; i< lastBallCount; i++) {
			ballArray[i].SetActive(false);
		}		
		Debug.Log("Set " + "Maze" +(currentLevel+1) + " inactive");
		mazes[currentLevel].SetActive(false);
		currentLevel = ((currentLevel+1) % 6);
		Debug.Log("Enable " + "Maze"+(currentLevel+1));
		mazes[currentLevel].SetActive(true);
		bucketSensorScript.ResetNumCollected();
		exit = mazes[currentLevel].transform.FindChild("Exit"+(currentLevel+1));
	}
		
	Time.timeScale = ballSpeed;
}