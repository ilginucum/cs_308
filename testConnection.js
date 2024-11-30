const { MongoClient } = require('mongodb');
const config = require('./appsettings.json'); // appsettings.json dosyanızdan bağlantı bilgilerini alır

async function testConnection() {
  const uri = config.ConnectionStrings.MongoDB; // appsettings.json'dan URI alır
  const client = new MongoClient(uri, { useNewUrlParser: true, useUnifiedTopology: true });

  try {
    await client.connect();
    console.log("Connected to MongoDB Atlas successfully!");
  } catch (error) {
    console.error("Failed to connect to MongoDB Atlas:", error);
  } finally {
    await client.close();
  }
}

testConnection();
