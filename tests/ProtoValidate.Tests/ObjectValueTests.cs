using System.Collections;
using buf.validate;
using Google.Protobuf.Reflection;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
using NUnit.Framework;

namespace ProtoValidate.Tests
{
    [TestFixture]
    public class ObjectValueTests
    {

        [Test]
        public void TestMapDescriptor()
        {

            var messageDescriptor = buf.validate.Theater.Descriptor;

            var fieldDescriptor = messageDescriptor.FindFieldByName("movieTicketPrice");
            var keyDescriptor = fieldDescriptor.MessageType.FindFieldByNumber(1);
            var valDescriptor = fieldDescriptor.MessageType.FindFieldByNumber(2);


            var t = new Theater();
            t.MovieTicketPrice.Add("movie 1", 10);
            t.MovieTicketPrice.Add("movie 2", 20);


            var field = t.MovieTicketPrice;
            IDictionary dict= (IDictionary) field;

            foreach (DictionaryEntry obj in dict)
            {
                
            }


        }
    }
}