using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows;
using System;
using System.IO;
using System.Security.Cryptography;

// ==============================================
// Script Overview: Server Communication & Data Handling
// ==============================================
// This script handles communication with an external server for:
// - **Testing Connection:** Verifies if the server is reachable.
// - **Uploading Data:** Encrypts and uploads experiment data securely.
// - **Encryption:** Uses AES encryption to secure data before transmission.
//
// Key Functions:
// - `testConnection()`: Sends a GET request to check server availability.
// - `Upload()`: Uploads encrypted data to the server.
// - `Encrypt(byte[] data)`: Encrypts data using AES before transmission.
//
// This ensures reliable server interaction and secure data handling.
// ==============================================


public class ConnectionHandler : MonoBehaviour
{

    // ---------------------------------------------------------- //
    // CONFIGURE: 
    // ---------------------------------------------------------- //
    private string serverAddress = "https://TutorialTemplate.eu.pythonanywhere.com";

    

    // ---------------------------------------------------------- //
    // other set up
    public GameObject ConnectionMenu;
    public TMPro.TextMeshProUGUI StatusText;
    public bool connectionOK = false;
    private int retryCount = 0;
    private int maxRetry = 3; // hof often are wr trying to reconect to server? 

    private string queriedID, encryptionKey;

    private string fileName_upload;
    private string filePath_upload;

 

    // find GameObject ConnectionMenu and assign to ConnectiobnMenu variable
    void Awake(){
        ConnectionMenu = GameObject.Find("ConnectionMenuCanvas"); // so we can turn it on and off
    }

    // ---------------------------------------------------------- //
    // CORE FUNCTIONS: 
    // ---------------------------------------------------------- //

    public IEnumerator testConnection(){

        /// ---------------- 
        // Function Name: testConnection()
        // Purpose: Attempts to connect to the external server and checks its reachability. Handles network/HTTP errors and retries if needed.
        // Return Value: IEnumerator (handles asynchronous requests via Unity's coroutine system)
        //
        // Explanation:
        // 1. Sends a GET request to the server at the provided serverAddress.
        // 2. Waits for the request to complete.
        // 3. If there's a network/HTTP error, checks retry count (up to 3 attempts) and shows a failure message with the option to retry.
        // 4. If the connection succeeds, logs the response and sets the subject number (ExperimentHandler.SubjectNumber), then activates the continue button.
        /// ---------------- 

        
        // Initiates a GET request to the specified server address:
        /*  - test the connection to an external server: check if the server is reachable and if it returns a valid response. */
        UnityWebRequest www = UnityWebRequest.Get(serverAddress);
        yield return www.SendWebRequest();

        // IN CASE OF NETWORK ERROR: (network error or an HTTP error during the request)
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            
            if (retryCount < maxRetry){ // tried connecting less than maxRetry times
                // notify participant that conection was not sucessful
                StatusText.SetText("Connection failed. Retrying...");

                // retry connection
                yield return new WaitForSeconds(4);  //wait 4 seconds before retrying
                StartCoroutine(testConnection());
                retryCount += 1; // keep track of how many times we tried to connect

            }
            else
            {
                // notify participant that conection was not sucessful and experiment cannot be started
                StatusText.SetText("Connection failed. There may be a problem with our servers, try again later.");
            }
        }
         // SUCCESSFUL CONNECTION: 
        else
        {
            // delete all of this?? disappears anyways. makes it easier to understand. 
            StatusText.SetText("Connection successful.");

            // get server response (format = {ID}: {encryption key})
            string serverResponse = www.downloadHandler.text;

            // 1. Store assigned ID
            //queriedID = www.downloadHandler.text.Substring(37);
            queriedID = serverResponse.Split(':')[0].Trim(); // ID is first part of server response

            // 2. store recieved encryption key
            encryptionKey = serverResponse.Split(':')[1].Trim(); // encryption key is second part of server response
            
            Debug.Log("----- Connection established -----");
            Debug.Log("Subject ID: " + queriedID);
            Debug.Log("Encryption Key: " + encryptionKey);
            // 3. signal that connection is established
            connectionOK = true; 

            // 4. disable connection menu (we are ready to start the experiment)
            displayMenu(false); 
        }
    }

    // helper function to pull assinged subject number
    public string getSubjectNumber(){
        return queriedID;
    }

    
    
    IEnumerator Upload()
    {
        /// ---------------- 
        // Function Name: Upload()
        // Purpose: Uploads the experiment data (including a filename) to the server. The data is encrypted before being uploaded.
        // The function handles both network/HTTP errors and retries the upload if it fails.
        // Parameters: None
        // Return Value: IEnumerator (handles asynchronous requests via Unity's coroutine system)
        //
        // Explanation:
        // 1. Retrieves the file name and reads the stored experiment data from the specified file path on participants device.
        // 2. Encrypts the  data.
        // 3. Combines the file name and encrypted data into a single byte array for uploading.
        // 4. Sends a PUT request to the server with the combined data.
        // 5. If there is a network or HTTP error, the function retries the upload (recursive call).
        /// ---------------- 

        
        // Retrieve the file name and read the file's bytes
        // We want to also upload the file name!
        byte[] filenameBytes = System.Text.Encoding.UTF8.GetBytes(fileName_upload); 
        

        // Read in temporarily stored data and then encrypt 
        byte[] dataBytes = System.IO.File.ReadAllBytes(filePath_upload); // read data from user PC
        byte[] encryptedFileBytes = Encrypt(dataBytes); // encrypt participant data

        // Combine the file name bytes and the encrypted file data into a single byte array
        byte[] uploadData = new byte[filenameBytes.Length + encryptedFileBytes.Length]; 
        System.Buffer.BlockCopy(filenameBytes, 0, uploadData, 0, filenameBytes.Length);
        System.Buffer.BlockCopy(encryptedFileBytes, 0, uploadData, filenameBytes.Length, encryptedFileBytes.Length);
        
        // Create a PUT request with the combined data 
        UnityWebRequest www = UnityWebRequest.Put(serverAddress, uploadData);
        yield return www.SendWebRequest(); // Wait for the upload to complete

         // Handle potential network or HTTP errors
        //if (www.isNetworkError || www.isHttpError)
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(www.error);
            StartCoroutine(Upload()); // Retry the upload in case of error
        }
        else
        {
            Debug.Log("Upload complete!"); // Notify that the upload was successful
        }
    }

    // helper fuction to trigger upload of data in ExperimentHandler
    public void UploadData(string fileName, string filePath){

        // which file are we uploading?
        fileName_upload = fileName;
        filePath_upload = filePath;

        // start uploading
        StartCoroutine(Upload());
    }



    public byte[] Encrypt(byte[] data)
    {
        /// ---------------- 
        // Function Name: Encrypt()
        // Purpose: Encrypts a byte array using AES encryption and returns the encrypted byte array.
        // This function uses a specified encryption key and initialization vector (IV) to perform AES encryption in CBC mode.
        // Parameters: 
        //   byte[] data - The byte array to be encrypted.
        // Return Value: 
        //   byte[] - The encrypted byte array.
        // 
        // Explanation:
        // 1. Creates an AES algorithm instance using Aes.Create().
        // 2. Sets the encryption key and IV using UTF-8 encoding for the provided key and IV strings.
        // 3. Creates an encryptor object using the AES algorithm's key and IV.
        // 4. Uses a MemoryStream to hold the encrypted data and a CryptoStream to perform the encryption.
        // 5. Writes the byte data to the CryptoStream, encrypting it as it’s written.
        // 6. Returns the encrypted byte array after flushing the final block of data.
        // --------

        // Create a new AES algorithm instance
        using (Aes aesAlg = Aes.Create())
        {
            // Set the encryption key and initialization vector (IV) using UTF-8 encoding
            aesAlg.Key = System.Text.Encoding.UTF8.GetBytes(encryptionKey);
            aesAlg.IV = GenerateRandomIV(); // Generate a random IV for each encryption

            // Create an encryptor object using the AES algorithm's key and IV
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Use a MemoryStream to hold the encrypted data
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                // Create a CryptoStream to perform the encryption
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    // Write the byte data to the CryptoStream, which encrypts it
                    csEncrypt.Write(data, 0, data.Length);

                    // Add padding if necessary and flush the final block of encrypted data
                    csEncrypt.FlushFinalBlock();
                }

            // Get the encrypted data from the MemoryStream
            byte[] encryptedData = msEncrypt.ToArray();

            // Prepend the IV to the encrypted data
            byte[] dataForUpload = new byte[aesAlg.IV.Length + encryptedData.Length];
            Buffer.BlockCopy(aesAlg.IV, 0, dataForUpload, 0, aesAlg.IV.Length); // copy IV to new array
            Buffer.BlockCopy(encryptedData, 0, dataForUpload, aesAlg.IV.Length, encryptedData.Length); // add data to new array after IV

            // Return the combined IV and encrypted data
            return dataForUpload;
            }
        }
    }

    // Generate a random initialization vector (IV) for AES encryption
    private byte[] GenerateRandomIV()
    {
        byte[] iv = new byte[16]; // 16 bytes for AES
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(iv);
        }
        return iv;
    }






    // ---------------------------------------------------------- //
    // HELPER FUNCTIONS FOR CONNECTION MENU
    // ---------------------------------------------------------- //

    // handle interactions with connection menu

    // function to set display menu to active or not
    public void displayMenu(bool display){

        // display connection menu
        ConnectionMenu.SetActive(display);
        
    }


    // tell ExperimetnHandler whether connection is established or not (so experimetn can start)
    public bool connectionCheck(){
        // implement small delay
    
        return (connectionOK);

        //Debug.Log(connectionOK);
    }


}