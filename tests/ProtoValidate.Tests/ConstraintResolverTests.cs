using System.Collections;
using buf.validate;
using Google.Protobuf.Reflection;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
using NUnit.Framework;

namespace ProtoValidate.Tests
{
    [TestFixture]
    public class ConstraintResolveTests
    {

        [Test]
        public void TestOptions()
        {

            var messageDescriptor = buf.validate.Transaction.Descriptor;
            var messageOptions = messageDescriptor.GetOptions();

            var messageExtension = Buf.Validate.ValidateExtensions.Message;

            var messageConstraints = messageOptions.GetExtension(messageExtension);
            
        }
    }
}