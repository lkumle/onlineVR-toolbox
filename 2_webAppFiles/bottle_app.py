# =========================================================== #
#  Template for Simple Data Collection Server (Bottle Framework) #
# =========================================================== #
#
# This script provides a minimal server implementation for researchers who need
# a simple way to allocate unique IDs and collect data files from remote sources.
# It is designed to work with Unity applications and can be customized for specific research needs.
#
# Usage:
# - follow step-by-step guide on how to set up server!
# - Configure `filename_length` to match Unity's filename encoding.
# - The server allocates unique IDs via a GET request.
# - Data files are received via a PUT request and stored locally.
#
# Authors: Alfie Brazier & Levi Kumle
# Last edited: 2025-03-04
#
# =========================================================== #

from bottle import Bottle, request

# ================================== #
# ================================== #
# Configure: enter lengths (number of bytes/characters) of file name here
# --> Align with unity.
filename_length = 35

#Encryption:
encryption_key = "tutorial-example"
# ================================== #


# =============================================== #
# # Prepare
# =============================================== #
# Create a Bottle application instance
application = Bottle()

# function  to allocate a unique ID
def get_next_id():
    """
    Reads the current ID from a file, increments it, and updates the file.
    Returns the allocated ID.
    """
    id_file_path = './mysite/id.txt'

    # Read the current ID from the file
    with open(id_file_path, 'r') as file:
        current_id = int(file.read().strip())
        print(f'Current ID: {current_id}')

    # Increment the ID and write it back to the file
    with open(id_file_path, 'w') as file:
        file.write(str(current_id + 1))

    return current_id


# =============================================== #
# # Define a GET endpoint to allocate a unique ID
# =============================================== #
@application.get('/')
def welcome():
    """
    Handles GET requests. Allocates a unique ID and encryption key and returns a response.
    """
    # get updated ID
    allocated_id = get_next_id()

    # return response
    # ensures the allocated_id is displayed as a 6-digit number, padded with leading zeros if necessary.
    response_message = f'{allocated_id:06d} : {encryption_key}'
    return response_message


# =============================================== #
# Define a PUT endpoint to receive and store data
# =============================================== #
@application.put('/')
def save_data():
    """
    Handles PUT requests. Extracts a user ID/file name from the request body and saves the
    remaining data into a file named after the user ID.
    """
    content = request.body.read()

    # Extract the first 41 bytes as the filenm (assuming UTF-8 encoding)
    user_id = content[:filename_length].decode('UTF-8')

    # Define the file path for saving data
    file_path = f'./mysite/files/{user_id}.bytes'

    # Write the remaining content to the file
    with open(file_path, 'wb') as file:
        file.write(content[filename_length:])

    return "Upload complete."

