// System 1
using System;
using System.IO;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Xml.Serialization;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

public class Client
{
    static void Main()
    {
        // Initialize your components and load RDF data
        TripleStore tripleStore = LoadRdfData("C:\\Users\\Anjana\\OneDrive\\Desktop\\wall-standard-case.ttl");

        // Update 1 - Set the name of a wall
        string wallId = "0czCsOQ5z4dg8QGBRFInu2";
        string wallName = "NewWallName";
        string newWallId = "9c44ef7d64d0418cbaf0765379b9d9be";


        XElement updateXml1 = UpdateWallName(wallId, wallName);
        XElement updateXml2 = AddNewWall(newWallId, tripleStore);
        XElement updateXml3 = AddOwnerHistory(tripleStore);
        XElement updateXml4 = AddPropertySetAndProperty();
        // Serialize the XML object to a string
        string serializedXml1 = updateXml1.ToString();
        string serializedXml2 = updateXml2.ToString();
        string serializedXml3 = updateXml3.ToString();
        string serializedXml4 = updateXml4.ToString();
        // Send the serialized XML to System 2 through TCP
        SendSerializedXmlToSystem2(serializedXml4);
    }

    private static void SendSerializedXmlToSystem2(string serializedXml)
    {
        TcpClient tcpClient = null;

        try
        {
            // Set the server IP address and port for System 2
            string serverIpAddress = "172.21.4.122";
            int port = 62962;

            // Connect to the server (System 2)
            tcpClient = new TcpClient(serverIpAddress, port);

            // Get the network stream
            NetworkStream networkStream = tcpClient.GetStream();

            // Convert the serialized XML string to bytes
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(serializedXml);

            // Send the content
            networkStream.Write(buffer, 0, buffer.Length);

            Console.WriteLine("Serialized XML sent to the Server");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            // Close the TcpClient
            tcpClient?.Close();
        }
    }

    private static TripleStore LoadRdfData(string filePath)
    {
        TripleStore tripleStore = new TripleStore();
        FileLoader.Load(tripleStore, filePath);
        return tripleStore;
    }

    private static XElement UpdateWallName(string wallId, string wallName)
    {
        string updateString = $@"PREFIX ifc: <http://standards.buildingsmart.org/IFC/DEV/IFC4/ADD1/OWL#>
                                PREFIX inst: <http://linkedbuildingdata.net/ifc/resources20200624_184152/>
                                PREFIX express: <https://w3id.org/express#>
                                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
                                INSERT DATA {{
                                    inst:NewWall rdf:type ifc:IfcWall ;
                                                 ifc:name_IfcRoot inst:IfcLabel_1337 ;
                                                 ifc:globalId_IfcRoot inst:NewWallId ;
                                                 express:hasString ""{wallName}"" .
                                }}";

        // Create XML representation for update query and metadata
        XElement updateXml1 = new XElement("UpdateQuery",
            new XElement("Metadata",
                new XElement("Timestamp", DateTime.Now)),
            new XElement("Update",
                new XElement("Query", updateString))
        );

        return updateXml1;
    }

    private static XElement AddNewWall(string newWallId, TripleStore tripleStore)
    {
        string updateString = $@"PREFIX ifc: <http://standards.buildingsmart.org/IFC/DEV/IFC4/ADD1/OWL#>
                                PREFIX inst: <http://linkedbuildingdata.net/ifc/resources20200624_184152/>
                                PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
                                INSERT DATA {{
                                    inst:{newWallId} rdf:type ifc:IfcWall ;
                                                   ifc:name_IfcRoot inst:IfcLabel_{Guid.NewGuid()} ;
                                                   ifc:globalId_IfcRoot inst:{newWallId} .
                                }}";

        // Create XML representation for update query and metadata
        XElement updateXml2 = new XElement("UpdateQuery",
            new XElement("Metadata",
                new XElement("Timestamp", DateTime.Now)),
            new XElement("Update",
                new XElement("Query", updateString))
        );

        return updateXml2;
    }

    private static XElement AddOwnerHistory(TripleStore tripleStore)
    {
        string updateString = @"PREFIX ifc:  <http://standards.buildingsmart.org/ifc/dev/ifc4/add1/owl#> 
                              PREFIX inst:  <http://linkedbuildingdata.net/ifc/resources/20240108_103400/> 
                              PREFIX express:  <https://w3id.org/express#> 
                              PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
                              INSERT DATA { 
                              inst:ifcproject_100 ifc:ownerhistory_ifcroot inst:ifcownerhistory_new . 
                              inst:ifcownerhistory_new a ifc:ifcownerhistory ; 
                              ifc:owninguser_ifcownerhistory inst:ifcpersonandorganization_3 ; 
                              ifc:owningapplication_ifcownerhistory inst:ifcapplication_1 ; 
                              ifc:changeaction_ifcownerhistory ifc:added . 
                              }";

        // Create XML representation for update query and metadata
        XElement updateXml3 = new XElement("UpdateQuery",
            new XElement("Metadata",
                new XElement("Timestamp", DateTime.Now)),
            new XElement("Update",
                new XElement("Query", updateString))
        );

        return updateXml3;
    }

    private static XElement AddPropertySetAndProperty()
    {
        string updateString = @"PREFIX ifc:  <http://standards.buildingsmart.org/ifc/dev/ifc4/add1/owl#>
                                PREFIX inst:  <http://linkedbuildingdata.net/ifc/resources20200624_184152/>
                                PREFIX express:  <https://w3id.org/express#>
                                PREFIX rdf:  <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                                PREFIX pset: <http://www.buildingsmart-tech.org/ifcowl/ifc4/psd#>

                                INSERT DATA {
                                  inst:yourentity ifc:haspropertysets inst:yourpropertyset .
                                  inst:yourpropertyset rdf:type ifc:ifcpropertyset ;
                                                     ifc:globalid_ifcroot inst:yourpropertysetid ;
                                                     ifc:ownerhistory_ifcroot inst:yourownerhistory ;
                                                     ifc:name_ifcroot ""yourpropertyset"" ;
                                                     ifc:description_ifcproperty ""yourpropertysetdescription"" ;
                                                     ifc:hasproperties inst:yourproperty .
                                  inst:yourproperty rdf:type ifc:ifcpropertysinglevalue ;
                                                    ifc:globalid_ifcroot inst:yourpropertyid ;
                                                    ifc:ownerhistory_ifcroot inst:yourownerhistory ;
                                                    ifc:name_ifcroot ""yourpropertyname"" ;
                                                    ifc:description_ifcproperty ""yourpropertydescription"" ;
                                                    ifc:nominalvalue_ifcproperty ""yournominalvalue"" .
                            }";

        // Create XML representation for update query and metadata
        XElement updateXml4 = new XElement("UpdateQuery",
            new XElement("Metadata",
                new XElement("Timestamp", DateTime.Now)),
            new XElement("Update",
                new XElement("Query", updateString))
        );

        return updateXml4;
    }


}