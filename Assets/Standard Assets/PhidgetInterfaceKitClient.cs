// --------------------------------------------------------------------------------
// class for connecting a phidgetinterfacekit into unity3d.com
// (direct implementation in c# not offical!)
// --------------------------------------------------------------------------------
//
// for more info look at:
// http://www.gametheory.ch/index.jsp?positionId=113660
//

/*
	attention: add servo-id manual

	used phidget-flash-implementation for template-connection 
	(PhidgetSocket > com/phidgets/PhidgetSocket.as: socketSend &  onSocketData: enable trace(); )

	async receive data.
	(run in background)
	
	bugs:
	- from time to time unity3d crashes (on play again the scene)
	- automatic change on new serial numbers of phidget (at the moment 1.02 ! in the code 1.01)
	
	todo:
	- plug&play routine that reconnects (every second try out)
	- remove
	
	- more than one phidget (every attachedphidget as an object ...)

	// .. 
	- plug&play routine that reconnects (every second try out)
	- remove
	
	// to do extre
	- more than one phidget (every attachedphidget as an object ...)
	possible:
		
	getValueByPhidget("8052","analog",0);
	getValueByPhidget("8077","analog",0);
	setDigitOutByPhidget("8011",3,1);	
	
*/
/*	
	// usage
	getValue("analog",0); 
	getValue("digital",0); 
	getValue("output",0); 

	setDigitOutput(3, 0); 
	

	// usage servo 
	// here 400 - 800

	getServoValue("engaged",0); 630-2300
	getServoValue("position",0); 630-2300

	setServoValue("engaged",3,1); // 630-2300
	setServoValue("position",3,630); // 630-2300
	*setServoValue("positionmin",3,630); // 630-2300
	*setServoValue("positionmax",3,2000); // 630-2300
	
	// example:
		// get
		var tempValue=GameObject.Find("PhidgetClientObject").GetComponent("PhidgetInterfaceKitClient").getValue("analog",0);
		// set
		GameObject.Find("PhidgetClientObject").GetComponent("PhidgetInterfaceKitClient").setDigitOutput(3,1);

*/
using UnityEngine;  
using System; 
using System.Collections;  
using System.Runtime.InteropServices;
// using System.IO.Ports; 

using System.Net;
using System.Net.Sockets;

using System.Text.RegularExpressions;


public class PhidgetInterfaceKitClient : MonoBehaviour 
{
	// ----------------------------------------
	// debug
	// ----------------------------------------
	public bool debug=false;
	private bool debugDirectWebserviceCommunication=false;
	public GUIStyle styleDebugObject;

	// ----------------------------------------
	// version
	// ----------------------------------------
	float version=1.12f; 
	// 1.0 added interfacekit
	// 1.1 added servo
	
	// > phidgetVersion ...

	
	// ----------------------------------------
	// servo
	// ----------------------------------------
	private String phidgetServoId="88446";



	// ----------------------------------------
	// status App
	// ----------------------------------------
	bool applicationQuits=false;
	
	// ----------------------------------------
	// status
	// ----------------------------------------
	bool statusConnectedToWebservice=false;
	
	// ----------------------------------------
	// phidgetClientWebservice
	// ----------------------------------------
	Socket serverObj; // connection
	IPEndPoint ipep;
	
		// ----------------------------------------
		// read
		// ----------------------------------------
		byte[] data = new byte[10240];
		int sizeData=10240;
		string input="", stringData="";
		string strInputToParse="";

	// ----------------------------------------
	// attached phidget object (only one!)
	// ----------------------------------------
	public bool flagEmulation=true;

	// GUI
	public GUIStyle styleObject;

	// ----------------------------------------
	// phidgetVersion look in the logs !!
	// ----------------------------------------
	// ...
	private string phidgetVersion="1.0.10";
	private string phidgetVersionGUI="";
	public GUIStyle styleAttention;

	// ----------------------------------------
	// init phidgettype
	// ----------------------------------------
	public bool flagPhidgetInterfaceKit=true;
	public bool flagPhidgetServo=true;
	
	int controlIndex=0;


	// the attached phidget
	
	// is one attached?
	bool flagPhidgetAttached=false;
		
	string serialNumber="";
	string phidgetType=""; 
	string phidgetLabel="";
	
	// PhidgetInterfaceKit 8/8/8
	int countInputOutput=8;
	bool setTrigger = true;
	
	int[] digitalOutput=new int[30];
	string[] digitalOutputDesc=new string[30];
	int[] digitalInput=new int[30];
	string[] digitalInputDesc=new string[30];
	int[] analogInput=new int[30];
	string[] analogInputDesc=new string[30];
	
	// PhidgetInterfaceServo 8
	string serialNumberServoDirect="88446"; // ... here
	string serialNumberServo="";
	int countServoInputOutput=8;
	int controlServoIndex=0;
	
	int[] digitalServoEngaged=new int[30];
	float[] analogServoPosition=new float[30];
	

	
/*
	int[] digitalServoPosition=new int[30];
	string[] digitalServoInputDesc=new string[30];
	
	int[] digitalServoOutput=new int[30];
	string[] digitalServoOutputDesc=new string[30];
	int[] digitalServoInput=new int[30];
	string[] digitalServoInputDesc=new string[30];
	int[] analogServoInput=new int[30];
	string[] analogServoInputDesc=new string[30];
*/
	
	// ----------------------------------------
	// WaitForData-Callback
	// ----------------------------------------
	AsyncCallback waitForDataCallback;
	
	// ----------------------------------------
	// Data
	// ----------------------------------------
	/*
		typeArt: "inputDigit"/"inputAnalog"
				 "outputDigit"
		
		getIntValue();
	
	*/
	
		// ------------------------------------------------
		// PhidgetInterfaceKit
		// ------------------------------------------------

		// ----------------------------------------
		// Actual Data
		// ----------------------------------------
		// typeArt "digital","analog", "output"
		// 
	public int getValue(string typeArt, int index)
		{
			// print("PhidgetInterfaceKitClient("+typeArt+","+index+")_");
			int retValue=-1;
			
			if ((index>=0)&&(index<countInputOutput))
			{
				// get value
				if (typeArt.Equals("digital")) { retValue=digitalInput[index];  }	
				if (typeArt.Equals("analog")) { retValue=analogInput[index];  }	
				if (typeArt.Equals("output")) { retValue=digitalOutput[index];  }	
			}
	
			return retValue;
		}	
		
			/*
				getNormalizedAnalogValue
				
				param: minValue - maxValue 
				returns:  a value 0-1 
				
				example: getNormalizedAnalogValue(2,710,900); 
				// sensor gives something between 710 and 100 and value
				// value = 800 > gives something like 0.5234 ... back
			*/
	public float getNormalizedAnalogValue( int index, float minValue, float maxValue )
	{
		float ret=-1;
		
		float val=getValue("analog", index);
		
		return getNormalizedValueForThis(  val,  minValue,  maxValue );	
	}
			
	public float getNormalizedLimitValue( float val, float minValue, float limitValue,  float maxValue )
	{
		float ret=0;
		
		// smaller
		if (val<limitValue)
		{
			ret=-1.0f*(1.0f-getNormalizedValueForThis(  val,  minValue,  limitValue ));
		}
		
		// bigger
		if (val>limitValue)
		{
			ret=getNormalizedValueForThis(  val,  limitValue,  maxValue );					
		}
		
		
		return ret;	
	}
			
	public float getNormalizedAnalogLimitValue( int index, float minValue, float limitValue,  float maxValue )
	{
		float ret=0;
		
		float val=getValue("analog", index);
						
		
		return getNormalizedLimitValue(  val,  minValue,  limitValue,   maxValue );	
	}

	public float getNormalizedValueForThis( float val, float minValue, float maxValue )
	{
		float ret=-1;
		
		if (val<minValue) val=minValue;
		if (val>maxValue) val=maxValue;
		
		float dif=maxValue-minValue;
		float valAbs=val-minValue;

		ret=valAbs/dif;

		return ret;	
	}			

	// set Digit ....		
	void setDigitOutput(int index, int val) // true / false 
	{
		updateValue("output", index, val);		
	}
	void setIntValue(int index, int val) // true / false 
	{
		updateValue("output", index, val);		
	}

	// set Digit ....		
	void setAnalogOutput(int index, int val) // 0-1000
	{
		analogInput[index]=val;	
	}
	
	// setAnalogFloatValue
	void setAnalogFloatValue(int index, float val) 
	{
		analogInput[index]=(int) (val*1024);	
	}
		
		
	// setOutputValue  
	// typeArt "digital","analog", "output"
	void updateValue(string typeArt,int index,int val)
	{
		if ((index>=0)&&(index<countInputOutput))
		{
			if (typeArt.Equals("digital")) { digitalInput[index]=val;  }	
			if (typeArt.Equals("analog")) { analogInput[index]=val;  }	
			if (typeArt.Equals("output")) { digitalOutput[index]=val;  }	
		}
		
		// update .. object
		if (typeArt.Equals("output"))
		{
			// send it to the host!
			// use the serial
			string cmd="set /PCK/PhidgetInterfaceKit/"+serialNumber+"/Output/"+index+"=\""+val+"\"";
			if (debugDirectWebserviceCommunication) print("*SEND: "+cmd);
			sendData(cmd);

		}	
		
	}
		
	// ------------------------------------------------
	// PhidgetInterfaceKit
	// ------------------------------------------------
	int getServoValue(string typeArt,int index)
	{
		if (typeArt.Equals("engaged"))
		{
			return digitalServoEngaged[index];
		}
		if (typeArt.Equals("position"))
		{
			return ((int) analogServoPosition[index]);
		}

		return -1;	
	}
		
		
	void setServoValue(string typeArt,int index, int val)
	{
		if (typeArt.Equals("engaged"))  
		{
			// set /PCK/PhidgetAdvancedServo/16902/Engaged/0="1"
			// string cmd="set /PCK/PhidgetAdvancedServo/"+serialNumberServo+"/Engaged/"+index+"=\""+val+"\"";
			string cmd="set /PCK/PhidgetAdvancedServo/"+serialNumberServoDirect+"/Engaged/"+index+"=\""+val+"\"";
			sendData(cmd);

		}
		
		if (typeArt.Equals("position"))
		{
			// set /PCK/PhidgetAdvancedServo/16902/Position/0="1480.0"
			// string cmd="set /PCK/PhidgetAdvancedServo/"+serialNumberServo+"/Position/"+index+"=\""+val+"\"";
			string cmd="set /PCK/PhidgetAdvancedServo/"+serialNumberServoDirect+"/Position/"+index+"=\""+val+"\"";
			sendData(cmd);

		}	
	
	}
				

	public bool getEmulation()
	{
		return flagEmulation;	
	}
		
	// ----------------------------------------
	// Start
	// ----------------------------------------
	void Start () 
	{
	  print("PhidgetInterfaceKitClient.Start()");
	  
	  // ...
	  for (int o=0;o<30;o++)
	  {
		  	digitalInput[o]=0;
		  	analogInput[o]=0;

		  	digitalOutput[o]=0;
	  }
	  
	  // desc
	  digitalInputDesc[0]="butt";
	  analogInputDesc[0]="temp";
	  digitalOutputDesc[0]="blink";
	  
	  // connect to phidgetWebService
	  connectToPhidgetWebService();
	}

	void setChangeTrigger (int triggerVal, int index)
	{
		string cmd="set /PCK/PhidgetInterfaceKit/"+serialNumber+"/Trigger/"+index+"=\""+triggerVal+"\"";
		if (debug) print("*SEND: "+cmd);
		sendData(cmd);
	}

	void connectToPhidgetWebService()
	{
		print("PhidgetInterfaceKitClient.connectToPhidgetWebService().Init.Start");
			
			// connect
		ipep = new IPEndPoint( IPAddress.Parse("127.0.0.1"), 5001);
		serverObj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		try
		{
		 serverObj.Connect(ipep);
		 // print("PhidgetInterfaceKitClient.Start() connected()");


		} catch (SocketException e)
		{
		 print("PhidgetInterfaceKitClient.CONNECT.couldnotconnect():Unable to connect to serverObj.");
		 print(e.ToString());
		 statusConnectedToWebservice=false;
		 return;
		}	  	

		try
		{
		 // print("PhidgetInterfaceKitClient.Start(): waitForData()");
		 WaitForData();
		 
		 // send init
		 sendData("need nulls");
		//      	 receiveData();
		 sendData("995 authenticate, version="+phidgetVersion);
		//      	 receiveData();
		 sendData("report 8 report");
		//     	 receiveData();


		 /*
		 	PhidgetInterfaceKit
		 */
		 // register PhidgetInterfaceKit
		 sendData("set /PCK/Client/0.0.0.0/90819/PhidgetInterfaceKit=\"Open\" for session");
		//     	 receiveData();
		 sendData("listen /PSK/PhidgetInterfaceKit lid0");
		//     	 receiveData();

		 /*
		 	PhidgetServo
		 */
		 // register PhidgetServo
		 
		 sendData("set /PCK/Client/0.0.0.0/"+phidgetServoId+"/PhidgetAdvancedServo=\"Open\" for session");
		 // receiveData();
		 sendData("listen /PSK/PhidgetAdvancedServo lid0");
		 // receiveData();

		// enable them all
		for (int z=0;z<8;z++)
		{
			// setServoValue("engaged",z, 1);
	  	// receiveData();
		}		 		
			
		// setServoValue("position",0, 1480);
		

		// check in answers!
		statusConnectedToWebservice=true;

		 // get type
	 // sendData("get 0");
						      	 
		print("PhidgetInterfaceKitClient.connected().Init.Start.End");		        

		} catch (SocketException e)
	      {
	         print("PhidgetInterfaceKitClient.SEND.couldnotconnect():Unable to connect to serverObj.");
	         print(e.ToString());
	         statusConnectedToWebservice=false;
	         return;
	      }	      
	}
	
	// ----------------------------------------
	// update
	// read as much as possible
	// (receive data)
	// ----------------------------------------
	void Update () 
	{
		// parseInt();
		if (strInputToParse.Length>20)
		{
			string[] valuesTmp = strInputToParse.Split(new char[] {'\0'});
			int count=0;
			foreach(string strValue in valuesTmp)
			{
				count++;
				
				// debug
				// print(strValue);
				
				// parse the string
				updateDataFromStream(strValue);
			
				//Set Change Trigger
				if (setTrigger && (serialNumber != ""))
				{
					setChangeTrigger(1,0);
					print("serial number is: " + serialNumber);
					setTrigger = false;
				}
			}
		
			// input to parse
			strInputToParse="";
		} // length	
		
		
	} // / Update
	
			
// ----------------------------------------
// Update Data from Stream
// ----------------------------------------
// parse them
void updateDataFromStream(string phidgetLine)
{
	
	// do the parsing
	/*
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/0 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/1 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/2 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/3 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/4 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/5 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/6 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/7 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Label latest value "das erste" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Name latest value "Phidget InterfaceKit 8/8/8" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/NumberOfInputs latest value "8" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/NumberOfOutputs latest value "8" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/NumberOfSensors latest value "8" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/0 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/1 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/2 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/3 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/4 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/5 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/6 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/7 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/0 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/1 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/2 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/3 latest value "341" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/4 latest value "0" (added)
		report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/5 latest value "0" (added)
	
	*/
	
	// no emulation
	if (!flagEmulation)
	{
			// ----------------------------------------
			// Digital Input
			// ----------------------------------------
			// report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Input/0 latest value "0" (added)
			// 2012: report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit//125671/Input/6 latest value "1" (changed)
//			string resultRegexInput=Regex.Replace(phidgetLine,@"^report 200-lid(.) is pending, key /PSK/PhidgetInterfaceKit//([0-9]+)/Input/([0-9]+) latest value ([^ ]+) (.+)$","found-$1-$2-$3-$4-$5");
			// print("result"+resultRegex);
//			string[] valuesTmpArr = resultRegexInput.Split(new char[] {'-'});
//			if (valuesTmpArr[0].Equals("found"))
//			{
//				serialNumber=valuesTmpArr[2];
//				int sensorIndex=Convert.ToInt32(valuesTmpArr[3]);
//				string strValueBr=valuesTmpArr[4];
//					   strValueBr=strValueBr.Substring(1,strValueBr.Length-2);
//					   // print("strValueBr: "+strValueBr);
//				int val=Convert.ToInt32(strValueBr);	
//				// added / removed
//				string strChange=valuesTmpArr[5];
//				
//				// updated
//				digitalInput[sensorIndex]=val;
//			}

			
			// ----------------------------------------
			// Analog Input (Sensor)
			// ----------------------------------------
			// report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/5 latest value "0" (added)
			// 2012: report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit//85030/Sensor/5 latest value "0" (added)
			string resultRegexSensor=Regex.Replace(phidgetLine,@"^report 200-lid(.) is pending, key /PSK/PhidgetInterfaceKit//([0-9]+)/Sensor/([0-9]+) latest value ([^ ]+) (.+)$","found-$1-$2-$3-$4-$5");
			// print("result"+resultRegex);
			string[] valuesTmpArr = resultRegexSensor.Split(new char[] {'-'});
			if (valuesTmpArr[0].Equals("found"))
			{
				serialNumber=valuesTmpArr[2];
				int sensorIndex=Convert.ToInt32(valuesTmpArr[3]);
				string strValueBr=valuesTmpArr[4];
					   strValueBr=strValueBr.Substring(1,strValueBr.Length-2);
					   // print("strValueBr: "+strValueBr);
				int val=Convert.ToInt32(strValueBr);	
				// added / removed
				string strChange=valuesTmpArr[5];
				
				// updated
				analogInput[sensorIndex]=val;
			} // analog
			
//			// ----------------------------------------
//			// Digitaler Output
//			// ----------------------------------------
//			// report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Output/0 latest value "0" (added)
//			// string resultRegexOutput=Regex.Replace(phidgetLine,@"^report 200-lid(.) is pending, key /PSK/PhidgetInterfaceKit/([0-9]+)/Output/([0-9]+) latest value ([^ ]+) (.+)$","found-$1-$2-$3-$4-$5");
//			// 2012: report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit//85030/Sensor/5 latest value "0" (added)
//			string resultRegexOutput=Regex.Replace(phidgetLine,@"^report 200-lid(.) is pending, key /PSK/PhidgetInterfaceKit//([0-9]+)/Output/([0-9]+) latest value ([^ ]+) (.+)$","found-$1-$2-$3-$4-$5");
//			// print("result"+resultRegex);
//			valuesTmpArr = resultRegexOutput.Split(new char[] {'-'});
//			if (valuesTmpArr[0].Equals("found"))
//			{
//				serialNumber=valuesTmpArr[2];
//				int sensorIndex=Convert.ToInt32(valuesTmpArr[3]);
//				string strValueBr=valuesTmpArr[4];
//					   strValueBr=strValueBr.Substring(1,strValueBr.Length-2);
//					   // print("strValueBr: "+strValueBr);
//				int val=Convert.ToInt32(strValueBr);	
//				// (added) / (remove)
//				string strChange=valuesTmpArr[5];
//					   strChange=strChange.Substring(1,strChange.Length-2);
//					   
//				// updated
//				digitalOutput[sensorIndex]=val;
//			} // analog	


//			// ----------------------------------------
//			// Servo Engaged 
//			// ----------------------------------------
//			// report 200-lid0 is pending, key /PSK/PhidgetInterfaceKit/85030/Sensor/5 latest value "0" (added)
//			 resultRegexSensor=Regex.Replace(phidgetLine,@"^report 200-lid(.) is pending, key /PSK/PhidgetAdvancedServo//([0-9]+)/Engaged/([0-9]+) latest value ([^ ]+) (.+)$","found-$1-$2-$3-$4-$5");
//			// print("result"+resultRegex);
//			valuesTmpArr = resultRegexSensor.Split(new char[] {'-'});
//			if (valuesTmpArr[0].Equals("found"))
//			{
//				
//				serialNumberServo=""+valuesTmpArr[2];
//				
//				int sensorIndex=Convert.ToInt32(valuesTmpArr[3]);
//				string strValueBr=valuesTmpArr[4];
//			   	strValueBr=strValueBr.Substring(1,strValueBr.Length-2);
//				// print("strValueBr: "+strValueBr);
//				int val=Convert.ToInt32(strValueBr);	
//				// added / removed
//				string strChange=valuesTmpArr[5];
//				
//				// updated
//				digitalServoEngaged[sensorIndex]=val;
//			} // analog
		 				
		 				
//			// ----------------------------------------
//			// Servo Position  
//			// ----------------------------------------
//			//report 200-lid0 is pending, key /PSK/PhidgetAdvancedServo/88446/Position/0 latest value "2.057163E+03" (current)
//			resultRegexSensor=Regex.Replace(phidgetLine,@"^report 200-lid(.) is pending, key /PSK/PhidgetAdvancedServo//([0-9]+)/Position/([0-9]+) latest value ([^ ]+) (.+)$","found-$1-$2-$3-$4-$5");
//			// print("result"+resultRegex);
//			valuesTmpArr = resultRegexSensor.Split(new char[] {'-'});
//			if (valuesTmpArr[0].Equals("found"))
//			{
//				
//				// serialNumberServo=valuesTmpArr[2];
//				int sensorIndex=Convert.ToInt32(valuesTmpArr[3]);
//				string strValueBr=valuesTmpArr[4];
//					   strValueBr=strValueBr.Substring(1,strValueBr.Length-2);
//// print("strValueBr: "+strValueBr);
//				float valFloat=float.Parse(strValueBr); // Convert.ToFloat32(strValueBr);	
//				
//				// added / removed
//				string strChange=valuesTmpArr[5];
//				
//				// updated
//				analogServoPosition[sensorIndex]=valFloat;
//			} // analog
			

				
		} // no emulation
			
	} // updateDataFromStream
	
			
	
	// OnGUI
    void OnGUI()
	{
		GUIStyle errorStyle = new GUIStyle();
		// connected ?
		if (true)
		{
			// show if there is a problem with phidgets
			// phidgetVersionGUI
			if (!phidgetVersionGUI.Equals(""))
			{
					Rect rectObj=new Rect(0,50,600,50);
					GUI.Box(rectObj,""+phidgetVersionGUI,styleAttention);
				
			}
			
			// connected
			if (statusConnectedToWebservice) 
			{
				/*
				// is a phidget there?
				if (!flagPhidgetAttached)
				{
					Rect rectObj=new Rect(10,20,300,50);
						GUIStyle style = new GUIStyle();
									style.alignment = TextAnchor.UpperLeft;
						GUI.Box(rectObj,"No PhidgetInsertKit is available.\nInsert a PhidgetInterfaceKit and restart game.\n(PhidgetInterfaceKit Webservice is runing.)",style);
				}
				*/
					
			} // connected

			// not connected
			if (!statusConnectedToWebservice) 
			{
				Rect rectObj=new Rect(10,20,450,250);
					errorStyle.alignment = TextAnchor.UpperLeft;
					errorStyle.fontSize = 20;
					errorStyle.normal.textColor = Color.red;
								
					GUI.Box(rectObj,"Phidget Webservice is not running.\n Please a) start service \nor b) install it from http://www.phidget.co.\nInsert a phidget-kit and restart game.",errorStyle);
								
			} // connected
				
			
		}
		// debug
		if (debug)
		{
			int leftPos=10;//Screen.width-300;
			
			// update automatique .. 
				string addOn="off";
				if (flagEmulation) addOn="on";
				if (GUI.Button(new Rect(leftPos+160,10,90,20),"emulation "+addOn))
				{
					flagEmulation=!flagEmulation;
				}
				
				// emulation .. 
				if (flagEmulation)
				{
					// digit ..
					for (int n=0;n<countInputOutput;n++)
					{
						addOn="";
						if (digitalInput[n]==1) { addOn="x"; }
						if (GUI.Button(new Rect(leftPos+160,50+n*13,17,13),""+addOn))
						{
							if (digitalInput[n]==1) { digitalInput[n]=0; } else {  digitalInput[n]=1;}
						
							// check it ..  
							// setDigitOutput(n, 1);
						}		
					}
					
					
					// control ..
					for (int n=0;n<countInputOutput;n++)
					{
						addOn="";
						int sizeButton=0;
						int sizeButtonY=0;
						if (controlIndex==n) { addOn=" <x> + <c> "; sizeButton=60; sizeButtonY=3; }
						if (GUI.Button(new Rect(leftPos+190,50+n*13,17+sizeButton,13+sizeButtonY),""+addOn))
						{
							controlIndex=n;
						}		
					}
					
					
					// on key ?
					if (Input.GetKey("x"))
					{
						analogInput[controlIndex]=analogInput[controlIndex]-8;
						if (analogInput[controlIndex]<0) analogInput[controlIndex]=0;
					}
					if (Input.GetKey("c"))
					{
						analogInput[controlIndex]=analogInput[controlIndex]+8;
						if (analogInput[controlIndex]>1023) analogInput[controlIndex]=1023;
					}
					
				} // emulation ?
			
			// display ?
			if (true)
			{ 
				string textStatus="PhidgetInterfaceKitClient\nVer:"+version+" Phidget-Ser: "+serialNumber+"\n";	
						textStatus=textStatus+"[dIn]\t[aIn]\t\t\t[dOut]   "+serialNumber+"\n";		
				// ...
				for (int n=0;n<countInputOutput;n++)
				{
					textStatus=textStatus+""+digitalInput[n]+"\t\t"+analogInput[n];				
					textStatus=textStatus+"\n";
				}

				Rect rectObj=new Rect(leftPos,10,200,400);
					GUIStyle style = new GUIStyle();
								style.alignment = TextAnchor.UpperLeft;
					GUI.Box(rectObj,textStatus,styleDebugObject);
				// display digitalOutput
				// desc
				textStatus="\n\n\n";
				for (int n=0;n<countInputOutput;n++)
				{
					textStatus=textStatus+digitalOutput[n];				
					textStatus=textStatus+"\n";
				}
				rectObj=new Rect(leftPos+110,10,400,400);
					GUI.Box(rectObj,textStatus,styleDebugObject);
				

				// desc
				textStatus="\n\n\n";
				for (int n=0;n<countInputOutput;n++)
				{
					textStatus=textStatus+""+digitalInputDesc[n]+"\t\t\t\t\t\t"+analogInputDesc[n]+"\t\t\t"+digitalOutputDesc[n];				
					textStatus=textStatus+"\n";
				}
				
				// servo
				// 
				textStatus=textStatus+"\n\nPhidgetInterfaceKitServo: Ser. "+serialNumberServo+"\n";				
				for (int n=0;n<countServoInputOutput;n++)
				{
					
					textStatus=textStatus+"\t\t\t"+digitalServoEngaged[n]+"\t\t\t\t"+analogServoPosition[n];				
					textStatus=textStatus+"\n";
				
					int sizeButton=0;
					int sizeButtonY=0;
						
					if (GUI.Button(new Rect(leftPos+20,50+144+n*13,17+sizeButton,13+sizeButtonY)," ",styleDebugObject))
					{
						// setServoValue("position",0, 1200);
						if (digitalServoEngaged[n]==0) setServoValue("engaged",n, 1);
						if (digitalServoEngaged[n]==1) setServoValue("engaged",n, 0);
						// setServoValue("position",1, 2200);
						controlServoIndex=n;
					}
					
					
				}
				
				// control ..
					for (int n=0;n<countServoInputOutput;n++)
					{
						addOn="";
						int sizeButton=0;
						int sizeButtonY=0;
						if (controlServoIndex==n) { addOn="<c> + <v> "; sizeButton=50; sizeButtonY=3; }
						if (GUI.Button(new Rect(leftPos+130,50+144+n*13,17+sizeButton,13+sizeButtonY),""+addOn))
						{
							controlServoIndex=n;
						}		
					}
					
					// on key ?
					if (Input.GetKey("c"))
					{
						analogServoPosition[controlServoIndex]=analogServoPosition[controlServoIndex]-10;
						if (analogServoPosition[controlServoIndex]<10) analogServoPosition[controlServoIndex]=10;
					    setServoValue("position",controlServoIndex,(int)analogServoPosition[controlServoIndex]); // 630-2300
	
					}
					if (Input.GetKey("v"))
					{
						analogServoPosition[controlServoIndex]=analogServoPosition[controlServoIndex]+10;
						if (analogServoPosition[controlServoIndex]>2560) analogServoPosition[controlServoIndex]=2560;
					    setServoValue("position",controlServoIndex,(int)analogServoPosition[controlServoIndex]); // 630-2300
						
					}
					

				// display
				rectObj=new Rect(leftPos-50,10,400,400);
					GUI.Box(rectObj,textStatus,styleDebugObject);
	
				
				
			}
			
				
			
		
		} // debug		
	}
	
	
	// ----------------------------------------
	// receiveData
	// ----------------------------------------
	void WaitForData()
	{
		try
		{
			// wait for data
			if (!applicationQuits)
			{
				// print("PhidgetInterfaceKitClient.WaitForData()");
				waitForDataCallback=new AsyncCallback(WaitForDataReceive);
				serverObj.BeginReceive(data,0, sizeData, SocketFlags.None,waitForDataCallback, serverObj );
			}
		}
		catch (Exception e)
		{
			 print("PhidgetInterfaceKitClient.WaitForData() Exception");
       	  	print(e.ToString());
       
		}
	
	}

	void WaitForDataReceive(IAsyncResult iar)
	{

	// print("PhidgetInterfaceKitClient.WaitForDataReceive");

		Socket remote=(Socket) iar.AsyncState;
		int recv=remote.EndReceive(iar);
		string stringData=System.Text.Encoding.ASCII.GetString(data,0, recv);
				
		if (stringData!="")
		{
			if (debugDirectWebserviceCommunication) print("PhidgetInterfaceKitClient.WaitForDataReceive ("+stringData+")");
			// BAD VERSION!!!
			if (stringData.IndexOf("Bad Version")!=-1) 
			{   
				Debug.LogError("ERROR VERSION! "+stringData); 
				Debug.LogError("CHANGE VERSION IN CODE!"); 
				// Take actual version here ...
				phidgetVersionGUI="WRONG PHIDGET VERSION (CHANGE IN CONFIG!): "+stringData;
				
			}
		}

		// add
		strInputToParse=strInputToParse+stringData;
		
		// echo it .. 
		// sendData(stringData);
		
		// recursive				
		WaitForData();
	}
				
	// ----------------------------------------
	// sendData
	// ----------------------------------------
	void sendData(string input)
	{
		if (serverObj!=null)
		{
			try
			{
			 	serverObj.Send(System.Text.Encoding.ASCII.GetBytes(input+"\n"));	
				if (debugDirectWebserviceCommunication) print("SEND: "+input);				 	
				}
				catch (SocketException e)
				{
					 print("PhidgetInterfaceKitClient.sendException():Unable to connect to serverObj.");
		       	 	 print(e.ToString());
		       
				}
			}
		}
	
	// ----------------------------------------
	// End
	// ----------------------------------------
	void OnApplicationQuit() 
	{
		print("PhidgetInterfaceKitClient.OnApplicationQuit()");
		applicationQuits=true;
		
		if (serverObj!=null)
		{
			try
			{
				
				serverObj.Shutdown(SocketShutdown.Both);
				serverObj.Close();
				
				// close correct?
				// waitForDataCallback=null;
				
			}
			catch (Exception e)
			{
				 print("PhidgetInterfaceKitClient.OnApplicationQuit() Exception");
	       	  print(e.ToString());
	       
			}
		
		}
	}


}
