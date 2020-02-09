# Stream Processing Using RabbitMQ

Stream processing is a messaging pattern where "producers" emit events to a stream and interested "consumers" or "listeners" can subscribe to the events without the producer having to be aware of who's listening, and the consumers don't have to care where the messages are coming from.

This setup allows us to decouple various components in our system and provides for a nice, consistent pattern of messaging between services.

I'm not going to go into too much detail on that here here, there have been many posts written on the subject already ([here's](https://dev.to/heroku/best-practices-for-event-driven-microservice-architecture-2lh7) a really good one).

This post is is going to show how this style of architecture can be implemented using .net core and RabbitMQ.

The goal isn't to create a real world example, just to show how to implement a fanout exchange as simply as possible.

## RabbitMQ
Short for Rabbit Message Queue, it's an open source message broker that we can use to facilitate communication between services in our system. There are a bunch of great [tutorials](https://www.rabbitmq.com/getstarted.html) on the different messaging patterns RMQ can be used to for, we're going to be focusing on the ["Publish/Subscribe"](https://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html) pattern, which uses the fanout style exchange.

To follow along with this post you're going to need to have an instance of RabbitMQ running so that we can send events to it, you can find installation instructions [here](https://www.rabbitmq.com/download.html).

If you're running docker you can enter

`docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management`

To spin up a new RabbitMQ server @ http://localhost:15672

The default credentials for this image *should* be guest/guest.

Feel free to poke around the tabs, there shouldn't be any activity but you can see that RMQ starts off with a number of default queues and the overview tab is nice.

#### Overview
![Overview](https://zachruffin.com/media/Blog/PostImages/StreamProcessing/Rmq%20Overview.jpg "Overview")

#### Default Exchanges
![Default Exchanges](https://zachruffin.com/media/Blog/PostImages/StreamProcessing/RmqDefaultExchanges.jpg "Overview")

## DotNet Core
For this demo we'll be using .net core 3.1 which, as of this writing, is the latest version of .net core, you can grab that [here](https://dotnet.microsoft.com/download/dotnet-core/3.1).

## The Producer
First we're going to build a really simple service that generates "Orders" for our client services to process. The order producer will contain
 * A list of inventory that it's allowed to order
 * A timer that is used to generate a new order for a random item with a random quantity every 1 second.
 * An event publisher, a helper class that pushes new order messages to the fanout exchange. 

 

## The Consumers
Keeping it simple we'll have the following consumers:
#### Order Quantity Counter 
Monitors the quantity of each order and applies fizzbuzz for each order.
  
#### Large Order Alert Service 
Monitors the quantity of each order and sends an alert for any order over 25 items.



## Data Models
#### Order
```
{
id:string,
productId:string,
quantity:number,
createdDate:DateTime,

}
```
#### Order Event
```
{
	eventName: string,
	orderData: Order
}
```


## Considerations
In the interest of keeping things simple, I'm going to organize all of these projects into a single solution and remove a lot of the additional code that one would normally have, like rate limiting, exception handling, service degradation, and all of that. Keeping all of that in this sample would, I think, get in the way.

That said, this is obviously not ready for production, so use at your own risk.