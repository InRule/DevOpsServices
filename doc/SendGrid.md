### SendGrid Setup

#### Account Creation

•	Azure Portal – create a resource for Twilio SendGrid using the plan of your choice.  Click the “Subscribe” button.  Follow the steps in the wizard until you are offered a button to complete your account setup with SendGrid.

•	Create your account directly from the SendGrid portal.

#### Create a Sender

SendGrid requires sender contact information including Name, email address and full mailing address.  This step prevents spammers from using the service.

#### API Keys

An API Key is required for CI/CD to interact with SendGrid.  Open “Settings” from the SendGrid portal.  Click on “API Keys.”  Click, “Create API Key.”  Once created, it will only be shown one time.  Copy it to the clipboard or into a safe location where it can be retrieved for later use.

#### IP Access Management

Also under “Settings”, click on the “IP Access Management” link.  You should already see your external IP address in use while creating your account.  Click on “+ Add IP Addresses”.  Open a new browser window and navigate to the setup area where you are hosting the CI/CD app service.  From the Azure portal, click on “Networking” for the App Service.  From the “Outbound Traffic” box, there is a section of “Outbound addresses”. Copy these to the clipboard and then paste them into the dialog from Send Grid. 

Your account should be working at this point and your API Key can be used for CI/CD configuration.
