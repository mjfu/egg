#pragma strict
private var phidgetScript : PhidgetInterfaceKitClient;
private var prevBendValue : int;
private var bendSensorValue : int;


function Start () {
	phidgetScript = GameObject.Find("PhidgetsInterfaceKit").GetComponent(PhidgetInterfaceKitClient);
}

function FixedUpdate () 
{
	var i : int;
	var sumDir : int;
		
	bendSensorValue = phidgetScript.getValue("analog",0);
	
	/* Remove jitter when trying to hold still */
//	if (prevPrevBendValue == bendSensorValue)
//		bendSensorValue = prevBendValue;
//	else if (bendSensorValue != prevBendValue) {
//		prevPrevBendValue = prevBendValue;
//		prevBendValue = bendSensorValue;	
//	}
 
	
	if (gameGUI.GetAutoROM()) {
		maxAperture = parseFloat(gameGUI.GetMaxBendSensor());
		minAperture = parseFloat(gameGUI.GetMinBendSensor());
		if (bendSensorValue > maxAperture) {
			maxAperture = bendSensorValue;
			gameGUI.SetMaxBendSensor(bendSensorValue);
		}
		else if ((bendSensorValue != 0) && (bendSensorValue < minAperture)) {
			minAperture = bendSensorValue;
			gameGUI.SetMinBendSensor(bendSensorValue);
		}
	}
	else {
		if (gameGUI.GetMaxBendSensor() != "") 			
			maxAperture = parseFloat(gameGUI.GetMaxBendSensor());
		if (gameGUI.GetMaxBendSensor() != "") 			
			minAperture = parseFloat(gameGUI.GetMinBendSensor());
	}

	apertureRange = maxAperture - minAperture;	
	
	//if (GameGUI.invert) {
		rotationZ = 380.0*parseFloat(bendSensorValue-minAperture)/parseFloat(apertureRange);
		angle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, rotationZ, rotateSpeed*Time.fixedDeltaTime);
		transform.localEulerAngles = Vector3(0,0,angle);
	//}
	//else {
	//	transform.position.y= -paddleFloor + paddleFloor*2.0*(bendSensorValue-minAperture)/apertureRange;
	//}
	
	if (gameGUI.GetInPlay()) {
		/* track number of direction changes to approximate the number of motor repetitions*/
		posRecord[arrayPos] = bendSensorValue;//angle;
		arrayPos++;
		if (arrayPos == 299) {
			for (i=0; i<arrayPos; i++) {
				velRecord[i] = (posRecord[i+1] - posRecord[i])/Time.deltaTime;
			}
			for (i=0; i<arrayPos-1; i++) {
				accelRecord[i] = (velRecord[i+1] - velRecord[i])/Time.deltaTime;
			}
			for (i=0; i<arrayPos-2; i++) {	
				jerkRecord[i] = (accelRecord[i+1]-accelRecord[i]);
			}
			sumDir = 0;
			for (i=2; i< arrayPos-2; i++) {
				/* Empirically, the 0.5 value is assuming Phidget Change Trigger = 2, use 1.0 for change trigger = 1 */
				if ((Mathf.Abs(jerkRecord[i]) > 0.0) && (Mathf.Abs(jerkRecord[i]) <= 0.75))
					sumDir = 1; 
			}
			if (sumDir == 1)
				numReps++;
			
			arrayPos = 0;
		}
	}
}