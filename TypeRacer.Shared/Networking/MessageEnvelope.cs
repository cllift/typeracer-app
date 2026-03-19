namespace TypeRacer.Shared.Enums;

//wraps a message so the reciever knows what type it is, and how to deserialise it
public class MessageEnvelope
{
        //the kind of message being sent
        public MessageType Type { get; set; }
        
        //The JSON payload of the actual message object
        public string Payload { get; set; } = string.Empty;
    
}