using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Diga.Core.Json.Test
{
    [TestClass]
    public class JsonTest
    {
        private const string TestJson = "{\"Name\":\"hallo\",\"Number\":10,\"Description\":\"Beschreibung\"}";
        private const string TestJsonNotAll = "{\"Name\":\"hallo\",\"Number\":10}";
        private const string TestJsonFomrated = "{\r\n \"Name\": \"hallo\",\r\n \"Number\": 10,\r\n \"Description\": \"Description\"\r\n}";
        private const string TestComplexJson = "{\"Number\":10,\"TestObject\":{\"Name\":\"hallo\",\"Number\":10,\"Description\":\"Beschreibung\"}}";
        private const string TestComplexJsonFomrated="{\r\n \"Number\": 10,\r\n \"TestObject\": {\r\n   \"Name\": \"hallo\",\r\n   \"Number\": 10,\r\n   \"Description\": \"Beschreibung\"\r\n  }\r\n}";


        [TestMethod]
        public void SerializeTest()
        {
            var obj = new TestObj();
            obj.Name = "hallo";
            obj.Number =10;
            obj.Description = "Beschreibung";
            string json = DigaJson.Serialize(obj);
            Assert.IsNotNull(json);
            Assert.AreEqual(TestJson, json);

        }
        [TestMethod]
        public void SerializeNotAllTest()
        {
            var obj = new TestObj();
            obj.Name = "hallo";
            obj.Number = 10;
            string json = DigaJson.Serialize(obj);
            Assert.IsNotNull(json);
            Assert.AreEqual(TestJsonNotAll , json);
        }
        [TestMethod]
        public void SerializeComplexNotAllTest()
        {
            var complex = new TestComplexObj
            {
                TestObject = new TestObj
                {
                    Name = "hallo"
                }
            };

            string json = DigaJson.Serialize(complex);
            Assert.IsNotNull(json);
            Assert.AreEqual("{\"TestObject\":{\"Name\":\"hallo\"}}", json);
        }

        [TestMethod]
        public void SerializeComplexTest()
        {
            var obj = new TestObj
            {
                Name = "hallo",
                Number = 10,
                Description = "Beschreibung"
            };
            var complex = new TestComplexObj
            {
                Number = 10,
                TestObject = obj
            };
            string json = DigaJson.Serialize(complex);
            Assert.IsNotNull (json);
            Assert.AreEqual (TestComplexJson, json);

        }
        [TestMethod]
        public void SerializeFormatTest()
        {
            var obj = new TestObj { Name ="hallo", Number =10 , Description="Description"};
                         
            string json = DigaJson.SerializeFormatted(obj);
            Assert.IsNotNull(json);
            Assert.AreEqual(TestJsonFomrated, json);



        }

        

        [TestMethod]
        public void SerializeComplexFormatTest()
        {
              var obj = new TestObj
            {
                Name = "hallo",
                Number = 10,
                Description = "Beschreibung"
            };
            var complex = new TestComplexObj
            {
                Number = 10,
                TestObject = obj
            };
            string json = DigaJson.SerializeFormatted(complex);
            Assert.IsNotNull (json);
            Assert.AreEqual (TestComplexJsonFomrated, json);
        }
        [TestMethod]
        public void DeserializeTest()
        {
            TestObj obj = DigaJson.Deserialize<TestObj>(TestJson);
            Assert.IsNotNull(obj);
            Assert.AreEqual("hallo", obj.Name);
            Assert.AreEqual(10, obj.Number);
            Assert.AreEqual("Beschreibung", obj.Description);

        }

        [TestMethod]
        public void DeserializeNotAllTest()
        {
            TestObj obj = DigaJson.Deserialize<TestObj>(TestJsonNotAll);
            Assert.IsNotNull(obj);
            Assert.AreEqual("hallo", obj.Name);
            Assert.AreEqual(10, obj.Number);
            Assert.IsNull(obj.Description);
        }

        [TestMethod]
        public void DeserializeFormatedTest()
        {
            TestObj obj = DigaJson.Deserialize<TestObj>(TestJsonFomrated);
            Assert.IsNotNull(obj);
            Assert.AreEqual("hallo", obj.Name);
            Assert.AreEqual(10, obj.Number);
            Assert.AreEqual("Description", obj.Description);

        }
    }
}