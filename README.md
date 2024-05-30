
# **installing RabbitMQ on Windows:**

### **Step 1: Install Erlang**
#### 1. Download Erlang:
* Download `otp_win64_xx.0.exe` the appropriate Erlang/OTP installer for your [Windows version](https://github.com/erlang/otp/releases).
 
*  [Check Erlang Version Requirements](https://www.rabbitmq.com/docs/which-erlang#compatibility-matrix)


### **Step 2: Install RabbitMQ**

#### 1. Download RabbitMQ:
* Download the RabbitMQ installer (EXE file)
  [rabbitmq-server-x.xx.xx.exe](https://github.com/rabbitmq/rabbitmq-server/releases)

##### 2. Install RabbitMQ:
* Run the RabbitMQ installer and follow the prompts.
* The default installation directory is usually
    ```shell
    c:\Program Files\RabbitMQ
    ```

### **Step 3: Set Up RabbitMQ Service**
#### 1. Install RabbitMQ Service:
* Open a command prompt as an administrator.
* Navigate to the RabbitMQ sbin directory, which is typically located at:
  ```shell
    cd C:\Program Files\RabbitMQ Server\rabbitmq_server-x.x.x\sbin
  ```
* Install the RabbitMQ service by running:
     ```shell
    rabbitmq-service.bat install
    ```

### **Step 4: Enable the RabbitMQ Management Plugin**
#### 1. Enable the Plugin:
* Still in the `sbin` directory, enable the RabbitMQ Management Plugin by running:
    ```shell
    rabbitmq-plugins.bat enable rabbitmq_management
    ```
#### 1. Enable the Plugin:
* Still in the `sbin` directory, enable the RabbitMQ Management Plugin by running:
  ```shell
    rabbitmq-plugins.bat enable rabbitmq_management
  ```
### **Step 5: Access the RabbitMQ Management Console**
#### 1. Open the Management Console:
* Open a web browser and go to `http://localhost:15672`.

##### 2. Log In:
* Use the default credentials: guest for the username and guest for the password.
* Navigate to the RabbitMQ sbin directory.
* Add a new user by running:
  ```shell
    rabbitmqctl.bat add_user myuser mypassword
  ```

### **Step 6: Create a New User (Optional but Recommended)**
#### 1. Add a New User:
* Open a command prompt as an administrator.
* Navigate to the RabbitMQ sbin directory.
* Add a new user by running:
 `rabbitmqctl.bat add_user myuser mypassword
  `
#### 2. Set User Tags and Permissions:
* Assign administrator tags to the new user:
  ```shell
  rabbitmqctl.bat set_user_tags myuser administrator
  ```
* Set the permissions for the new user:
  ```shell
  rabbitmqctl.bat set_permissions -p / myuser ".*" ".*" ".*"
  ```

Now RabbitMQ should be installed, configured, and running on your Windows system. You can manage it through the web-based RabbitMQ Management Console at `http://localhost:15672`.

# DOTNET Configuration 

**Prerequisites**
This tutorial assumes RabbitMQ is installed and running on localhost on the standard port (5672). In case you use a different host, port or credentials, connections settings would require adjusting.

**Setup**
First let's verify that you have .NET Core toolchain in `PATH`:
 
```bash
dotnet --help
```
 
should produce a help message.

Now let's generate two projects, one for the publisher and one for the consumer:

```bash
dotnet new console --name Send
mv Send/Program.cs Send/Send.cs
dotnet new console --name Receive
mv Receive/Program.cs Receive/Receive.cs
```

This will create two new directories named Send and Receive.

Then we add the client dependency.
```shell
cd Send
dotnet add package RabbitMQ.Client
cd ../Receive
dotnet add package RabbitMQ.Client
```
Now we have the .NET project set up we can write some code.

 We'll call our message publisher (sender) `Send.cs` and our message consumer (receiver) `Receive.cs.` The publisher will connect to RabbitMQ, send a single message, then exit.
 ```csharp
using System.Text;
using RabbitMQ.Client;
```
then we can create a connection to the server:

```csharp
using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
```
The connection abstracts the socket connection, and takes care of protocol version negotiation and authentication and so on for us. Here we connect to a RabbitMQ node on the local machine - hence the localhost. If we wanted to connect to a node on a different machine we'd simply specify its hostname or IP address here.

Next we create a channel, which is where most of the API for getting things done resides.

To send, we must declare a queue for us to send to; then we can publish a message to the queue:

```csharp
using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "hello",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

const string message = "Hello World!";
var body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(exchange: string.Empty,
                     routingKey: "hello",
                     basicProperties: null,
                     body: body);
Console.WriteLine($" [x] Sent {message}");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
```
Declaring a queue is idempotent - it will only be created if it doesn't exist already. The message content is a byte array, so you can encode whatever you like there.

When the code above finishes running, the channel and the connection will be disposed. That's it for our publisher.

**Sending doesn't work!**
If this is your first time using RabbitMQ and you don't see the "Sent" message then you may be left scratching your head wondering what could be wrong. Maybe the broker was started without enough free disk space (by default it needs at least 50 MB free) and is therefore refusing to accept messages. Check the broker logfile to confirm and reduce the limit if necessary. The [configuration file documentation](https://www.rabbitmq.com/configure.html#config-items "configuration file documentation") will show you how to set `disk_free_limit`.

**Receiving**
As for the consumer, it is listening for messages from RabbitMQ. So unlike the publisher which publishes a single message, we'll keep the consumer running continuously to listen for messages and print them out.

The code (in `Receive.cs`) has almost the same `using` statements as `Send`:
```csharp
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
```

Setting up is the same as the publisher; we open a connection and a channel, and declare the queue from which we're going to consume. Note this matches up with the queue that `Send` publishes to.
```csharp
var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "hello",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
...
```

Note that we declare the queue here as well. Because we might start the consumer before the publisher, we want to make sure the queue exists before we try to consume messages from it.

We're about to tell the server to deliver us the messages from the queue. Since it will push us messages asynchronously, we provide a callback. That is what `EventingBasicConsumer.Received` event handler does.

```csharp
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "hello",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}");
};
channel.BasicConsume(queue: "hello",
                     autoAck: true,
                     consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
```

**Putting It All Together**
Open two terminals.

You can run the clients in any order, as both declares the queue. We will run the consumer first so you can see it waiting for and then receiving the message:

```shell
cd Receive
dotnet run
```

Then run the producer:
```shell
cd Send
dotnet run
```

The consumer will print the message it gets from the publisher via RabbitMQ. The consumer will keep running, waiting for messages, so try restarting the publisher several times.
