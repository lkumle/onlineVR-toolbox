
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

// this script is responsible for handling the experiment logic
// experiment logic is defined in ExperimentSequence() coroutine
//  - 1. connect to serer
// - 2. run two mock blocks and upload data at the end of the block 

public class ExperimentHandler : MonoBehaviour
{
       
   // --------------------------------------------------------------------------------------------
   // VARIABLES      -----------------------------------------------------------------------------

    public ConnectionHandler ConnectionHandler; // connection handler script
    public static string SubjectNumber; // subject number (will be assigned by server)
  
    // for data writing/saving
    private StreamWriter localDataFile;
    public static string filePath, fileName;


    // for mock tutorial room
    public GameObject BasicScene; // basic room to display at the start


    // --------------------------------------------------------------------------------------------
    // RUN ---------------------------------------------------------------------------------------
    void Awake()
    {
        // ------------------------------- // 
        // SETUP
        // ------------------------------- //

        // general useful things for testing online: 
        // make sure that culture is set to invariant (for csv writing)
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

      
        // hide experiment environment at the start 
        // we want to start with the connection menu and only allow participants to proceed if connection is established
        BasicScene = GameObject.Find("BasicScene"); // so we can turn it on and off
        BasicScene.SetActive(false); 
        // --> adjust this to your actual experiment environment (e.g. hide all objects that should not be visible at the start)

        // ------------------------------- //
        // START EXPERIMENT
        // ------------------------------- //
        StartCoroutine(ExperimentSequence()); // 
    }


    // ------------------------------------------------------------------------------------------------ // 
    // Coroutine determining Experiment sequence
    // ------------------------------------------------------------------------------------------------ //
    private IEnumerator ExperimentSequence(){

        // ==================================================== //
        // ------ 1: establish connection with web server ----- //
        // ==================================================== //

        // display connection menu: This notifies participants that the connection is being established
        ConnectionHandler.displayMenu(true); 

        // Start the testConnection coroutine and wait for it to complete
        // --> connects to server and queeries the server for the subject number and encryption key
        yield return StartCoroutine(ConnectionHandler.testConnection());

        // Check if connection was established successfully 
        // -> terminate experiment if connection failed
        bool  connectionStatus = ConnectionHandler.connectionCheck();

        if(!connectionStatus){
           // stop coroutine
            Debug.Log("Connection failed. Stopping experiment.");
            yield break;
        }
        
        // queery assigned subject number from connection handler 
        SubjectNumber = ConnectionHandler.getSubjectNumber();


        
        // ==================================================== //
        // -------------- 2: run experiment ------------------- //
        // ==================================================== //

        //     !! Replace with your actual experiment logic !!

        // set up mock environent
        BasicScene.SetActive(true);

        // "run" two blocks
        for(int block = 1; block <= 2; block++){

            // ------------------------- //
            // SET UP DATA FILE
            // ------------------------- //  

            // 1. create filename and filepath for data file
            // our file path has this format: 
            // "onlineVR_{subjectNumber}_{date-time}_B{block}.csv"
            fileName = String.Format("onlineVR_{0}_{1}_B{2}", 
                                    SubjectNumber, 
                                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm"),
                                    block.ToString());

            // 2. we want to write to persistentDataPath!
            filePath = Path.Combine(Application.persistentDataPath,  fileName  + ".csv"); 


            // ------------------------- //
            // WRITE DATA 
            // ------------------------- //
            localDataFile = new StreamWriter(filePath); // create file at specified path

            localDataFile.WriteLine("Well done, you have successfully decrypted this data file."); // write into file


            // ------------------------- //
            // UPLOAD DATA TO SERVER
            // ------------------------- //
            localDataFile.Close(); // close file

            ConnectionHandler.UploadData(fileName, filePath); // upload data to server
    

            //wait 5 seconds in between "blocks" (this is just for mock purposes)
            //yield return new WaitForSeconds(5);

            //Debug.Log("Block " + block + " finished");

        }
    }  
}
