# Nopcommerce Bamboocard Task Dockerized Deployment

This project provides an easy-to-deploy, containerized version of NopCommerce, with all the necessary configurations, including database and plugins.

## Prerequisites

- Docker Desktop installed on your local machine (or use Docker on a server).

## Steps to Build and Run the Containerized Application

1. **Clone the Repository:**
   Clone nopcommerce-task-bamboocard GitHub repository to your local machine.

   ```bash
   git clone https://github.com/bhautikpatel1907/nopcommerce-task-bamboocard.git
   cd nopcommerce-task-bamboocard
   ```

2. **Build and Run the Docker Containers:**
   Ensure Docker Desktop is running, then build and start the containers.

   ```bash
   docker-compose up --build
   ```

   This command will:
   - Build the NopCommerce application image from the `Dockerfile`.
   - Start two containers:
     - `nopcommerce_web` (NopCommerce application)
     - `nopcommerce_database` (SQL Server)
   - Expose the NopCommerce application on port `8080`.

3. **Access the Application:**
   Once the containers are up and running, open your browser and navigate to:

   ```bash
   http://localhost:8080
   ```

   Follow the installation wizard to complete the setup of your NopCommerce store.

   During installation:
   - **Database Server:** Use `nopcommerce_mssql_server` (container name for the database).
   - **Username:** `sa`
   - **Password:** `pass@word1`

4. **Verify Application:**
   After the setup is complete, you can verify that the NopCommerce application is running by accessing it at `http://localhost:8080`.

## Container Configuration

- **Web Application Container (`nopcommerce_web`)**
  - Ports: `8080:5000`
  - Dependencies: `nopcommerce_database`

- **Database Container (`nopcommerce_database`)**
  - Ports: `1433:1433`
  - Environment:
    - `SA_PASSWORD`: `pass@word1`
    - `ACCEPT_EULA`: `Y`
    - `MSSQL_PID`: `Express`

## How to Test the API Endpoint

1. **Generate JWT Token:**
   - Add username and password parameters in the API token request.
   - Test API requests using Postman or any HTTP client.

2. **Testing the API:**
   You can find the Postman collection in the scr/AppData/BambooCards-api.postman_collection.json. Import it into your Postman to test API endpoints, such as authentication & order retrieval.

## Docker Commands

- To stop the containers:

  ```bash
  docker-compose down
  ```

- To remove the containers and images:

  ```bash
  docker-compose down --rmi all
  ```
- ----------------------------------------
- All 4 Task Description:
Task 1:
Plugin info: DiscountRequirement.MustBeAssignToCustomerHavingThreeOrMoreOrders - https://prnt.sc/ydDipXXU6k0t
On Install:
  - Add discount - https://prnt.sc/lSCMeJOfLFlY , https://prnt.sc/4uX1WUAzi7un
  - Add discount requirnment and group 
  - Add Resource strings
On Uninstall:
  - Remove discount
  - Remove discount requirnment and group 
  - Remove Resource strings

Note: 
- A discount will be applied to the customer who's having 3 or more orders with PAID status. - https://prnt.sc/f9Q4uC4MdPFf
- I know there was a requirement of adding a setting to enable/disable the plugin & set a percentage. But the requirement is already satisfied as we add the discount. 
- I have created a "Discount requirements" type plugin because it is the best way to process a discount in NopCommerce.
- We can also create a Mic-type plugin with a configuration page & set a 10% on the order total calculation service, but I think having a discount-type plugin is the best approach.

==================
Task 2: Modify the Checkout Process
- Added gift message field at orde confirm screen -  
- Shows in Admin/Order/Edit - https://prnt.sc/y6xl91g9sw-l
- Shows in Public/OrderDetail - https://prnt.sc/EFw71NkOiWFB

=====================
Task 3: Allow to search by "Name" on the product attribute page (admin area)
- Added search panel - https://prnt.sc/jP2lfnwLg90l 

=====================
Task 4. API Development (Order Retrieval)
Plugin info: Nop.Plugin.Misc.Api
Develop a simple API endpoint in NopCommerce for retrieving order details.
- Developed API Plugin with JWT auth scheme and configure page - https://prnt.sc/jSQMOTkUKXt5
- Plugin api will only work if valid bearer token is passed and plugin is enabled. 
- Enable/Disable plugin - https://prnt.sc/rkwHbTc8BeyV
- API sample
  1) https://localhost:59579/api/token - To get access  token - https://prnt.sc/_o0a2W8G52_Z
  2) https://localhost:59579/api/orders/admin@yourstore.com - To fetch customer orders - https://prnt.sc/nyPRGt0AgBoZ
  
==============================================================================================================
- Additional note:
    The application was published in Release mode using deploy.cmd, and the release artifact is located in src/artifacts/wwwroot. (just deploy and install app, API & Custom Discount plugin will be automatically installed)