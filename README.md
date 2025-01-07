# Simple Config Server

A simple server for managing and distributing JSON and text-based configurations, ideal for applications such as IoT device management or local network configuration.

The server supports creating, updating, retrieving, and deleting JSON or plain text configuration files identified by unique IDs.

The container for this application is available on Docker Hub:
- For `linux/amd64` architectures: `docker pull nightlyprojects/simple-config-server:amd64-latest`
- For `linux/arm64` architectures: `docker pull nightlyprojects/simple-config-server:arm64-latest`

## Features
- **JSON Management**: Store, retrieve, update, and delete JSON configurations.
- **Text File Support**: Manage plain text files similarly to JSON files.
- **Validation**: Ensures identifiers adhere to a strict naming pattern.
- **Logging**: Logs all actions for debugging and monitoring.
- **Extensibility**: Designed with flexibility for future expansions.

## Usage

### Endpoints
The server exposes RESTful HTTP endpoints for managing files. Each file is identified by a unique `id` that adheres to the following pattern: `^[a-zA-Z0-9][a-zA-Z0-9\-_.]*$`.

### JSON File Operations
1. **Retrieve JSON Data**
   - **Endpoint**: `GET /json`
   - **Query Parameter**: `id` (required) - The unique identifier of the file.
   - **Response**: Returns the JSON content if the file exists.

2. **Create New JSON File**
   - **Endpoint**: `POST /json`
   - **Query Parameter**: `id` (required) - The unique identifier for the new file.
   - **Body**: Valid JSON content.
   - **Response**: Creates the file or returns a conflict if the file exists.

3. **Update JSON File**
   - **Endpoint**: `PUT /json`
   - **Query Parameter**: `id` (required) - The unique identifier of the file.
   - **Body**: Valid JSON content.
   - **Response**: Updates the file content or creates a new file if it doesn’t exist.

4. **Delete JSON File**
   - **Endpoint**: `DELETE /json`
   - **Query Parameter**: `id` (required) - The unique identifier of the file.
   - **Response**: Deletes the file or returns an error if it doesn’t exist.

### Text File Operations
1. **Retrieve Text Data**
   - **Endpoint**: `GET /text`
   - **Query Parameter**: `id` (required) - The unique identifier of the file.
   - **Response**: Returns the text content if the file exists.

2. **Create New Text File**
   - **Endpoint**: `POST /text`
   - **Query Parameter**: `id` (required) - The unique identifier for the new file.
   - **Body**: Plain text content.
   - **Response**: Creates the file or returns a conflict if the file exists.

3. **Update Text File**
   - **Endpoint**: `PUT /text`
   - **Query Parameter**: `id` (required) - The unique identifier of the file.
   - **Body**: Plain text content.
   - **Response**: Updates the file content or creates a new file if it doesn’t exist.

4. **Delete Text File**
   - **Endpoint**: `DELETE /text`
   - **Query Parameter**: `id` (required) - The unique identifier of the file.
   - **Response**: Deletes the file or returns an error if it doesn’t exist.

## Running the Server

### Environment Variables
- `DATA_DIR`: Specifies the root directory for storing files (default: `data`). 
Don't use the variable to point to a directory outside of the container. Use the docker mechanics to mount the volume to `/data`.
- `LOG_LEVEL`: Sets the logging verbosity (default: `Information`).

### Docker Usage
To run the server using Docker:

1. Pull the appropriate image for your architecture:
   ```sh
   docker pull nightlyprojects/simple-config-server:amd64-latest
   docker pull nightlyprojects/simple-config-server:arm64-latest
   ```

2. Create the working directory outside the container, if you wish to access it from the outside and store persistent data.
It should have the following structure: (If you don't provide the sub-folders, the program will create them for you)
    ```
    data/
    ├─ storage/
    │  ├─ json/
    │  │  ├─ your-config-files.json
    │  ├─ text/
    │  │  ├─ your-config-files.txt
    ├─ logs/
    ``` 

3. Run the container:
   ```sh
   docker run -d -p 24025:24025 \
      -v $(pwd)/data:/data \
      nightlyprojects/simple-config-server:amd64-latest
   ```
   Replace `$(pwd)/data` with the desired host directory for data storage.
   Replace `24025` with your desired port and select the right image for your architecture.

## Examples

### Create a JSON Configuration
```sh
curl -X POST "http://localhost:24025/json?id=example" \
     -H "Content-Type: application/json" \
     -d '{"key":"value"}'
```

### Retrieve the JSON Configuration
```sh
curl "http://localhost:24025/json?id=example"
```

### Update the JSON Configuration
```sh
curl -X PUT "http://localhost:24025/json?id=example" \
     -H "Content-Type: application/json" \
     -d '{"key":"newValue"}'
```

### Delete the JSON Configuration
```sh
curl -X DELETE "http://localhost:24025/json?id=example"
```
For the txt-files, this works the same way. Just use the `/text` endpoint instead of the `/json` endpoint. 

---
## Security Notice

This server does not implement any security measures. It lacks:

- Authentication and authorization.
- Encryption (e.g., HTTPS).
- Overwrite protection for files.
- Content type validation for the text-endpoint.

Use this server in secure and controlled environments only. Avoid deploying it directly on public networks without additional security layers.