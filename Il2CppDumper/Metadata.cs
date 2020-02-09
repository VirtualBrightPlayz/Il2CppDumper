using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Il2CppDumper
{
    public class OffsetAttribute : Attribute
    {
        
    }

    public sealed class Metadata : MyBinaryReader
    {
        public Il2CppGlobalMetadataHeader metadataHeader;
        public Il2CppImageDefinition[] imageDefs;
        public Il2CppTypeDefinition[] typeDefs;
        public Il2CppMethodDefinition[] methodDefs;
        public Il2CppParameterDefinition[] parameterDefs;
        public Il2CppFieldDefinition[] fieldDefs;
        private Il2CppFieldDefaultValue[] fieldDefaultValues;
        private Il2CppParameterDefaultValue[] parameterDefaultValues;
        public Il2CppPropertyDefinition[] propertyDefs;
        public Il2CppCustomAttributeTypeRange[] attributeTypeRanges;
        private Il2CppStringLiteral[] stringLiterals;
        private Il2CppMetadataUsageList[] metadataUsageLists;
        private Il2CppMetadataUsagePair[] metadataUsagePairs;
        public int[] attributeTypes;
        public int[] interfaceIndices;
        public Dictionary<uint, SortedDictionary<uint, uint>> metadataUsageDic;
        public long maxMetadataUsages;
        public int[] nestedTypeIndices;
        public Il2CppEventDefinition[] eventDefs;
        public Il2CppGenericContainer[] genericContainers;
        public Il2CppFieldRef[] fieldRefs;
        public Il2CppGenericParameter[] genericParameters;
        public int[] constraintIndices;
        //public int[] exportedTypeDefinitionsOffsets;
        //public int[] windowsRuntimeTypeNamesOffsets;
        public static bool running = false;
        public static Metadata inst;
        public Stream fsstream;
        public ulong globaloffset;
        public string FileData = "";
        public long currentDataOffset;

        public Metadata(Stream stream, float version, string ogstring, string newstring, string filename) : base(stream)
        {
            inst = this;
            Debug.WriteLine(GetEncodedIndexType(327680));
            //return;
            this.version = version;
            Debug.WriteLine(Position);
            metadataHeader = ReadClass<Il2CppGlobalMetadataHeader>();
            Debug.WriteLine(Position);
            if (metadataHeader.sanity != 0xFAB11BAF)
            {
                throw new Exception("ERROR: Metadata file supplied is not valid metadata file.");
            }
            switch (metadataHeader.version)
            {
                case 16:
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                    break;
                default:
                    throw new Exception($"ERROR: Metadata file supplied is not a supported version[{version}].");
            }
            imageDefs = ReadMetadataClassArray<Il2CppImageDefinition>(metadataHeader.imagesOffset, metadataHeader.imagesCount);
            typeDefs = ReadMetadataClassArray<Il2CppTypeDefinition>(metadataHeader.typeDefinitionsOffset, metadataHeader.typeDefinitionsCount);
            methodDefs = ReadMetadataClassArray<Il2CppMethodDefinition>(metadataHeader.methodsOffset, metadataHeader.methodsCount);
            parameterDefs = ReadMetadataClassArray<Il2CppParameterDefinition>(metadataHeader.parametersOffset, metadataHeader.parametersCount);
            fieldDefs = ReadMetadataClassArray<Il2CppFieldDefinition>(metadataHeader.fieldsOffset, metadataHeader.fieldsCount);
            fieldDefaultValues = ReadMetadataClassArray<Il2CppFieldDefaultValue>(metadataHeader.fieldDefaultValuesOffset, metadataHeader.fieldDefaultValuesCount);
            parameterDefaultValues = ReadMetadataClassArray<Il2CppParameterDefaultValue>(metadataHeader.parameterDefaultValuesOffset, metadataHeader.parameterDefaultValuesCount);
            propertyDefs = ReadMetadataClassArray<Il2CppPropertyDefinition>(metadataHeader.propertiesOffset, metadataHeader.propertiesCount);
            interfaceIndices = ReadClassArray<int>(metadataHeader.interfacesOffset, metadataHeader.interfacesCount / 4);
            nestedTypeIndices = ReadClassArray<int>(metadataHeader.nestedTypesOffset, metadataHeader.nestedTypesCount / 4);
            eventDefs = ReadMetadataClassArray<Il2CppEventDefinition>(metadataHeader.eventsOffset, metadataHeader.eventsCount);
            genericContainers = ReadMetadataClassArray<Il2CppGenericContainer>(metadataHeader.genericContainersOffset, metadataHeader.genericContainersCount);
            genericParameters = ReadMetadataClassArray<Il2CppGenericParameter>(metadataHeader.genericParametersOffset, metadataHeader.genericParametersCount);
            constraintIndices = ReadClassArray<int>(metadataHeader.genericParameterConstraintsOffset, metadataHeader.genericParameterConstraintsCount / 4);
            if (version > 16)
            {
                //running = true;
                stringLiterals = ReadMetadataClassArray<Il2CppStringLiteral>(metadataHeader.stringLiteralOffset, metadataHeader.stringLiteralCount);
                //running = false;
                metadataUsageLists = ReadMetadataClassArray<Il2CppMetadataUsageList>(metadataHeader.metadataUsageListsOffset, metadataHeader.metadataUsageListsCount);
                metadataUsagePairs = ReadMetadataClassArray<Il2CppMetadataUsagePair>(metadataHeader.metadataUsagePairsOffset, metadataHeader.metadataUsagePairsCount);

                ProcessingMetadataUsage();

                fieldRefs = ReadMetadataClassArray<Il2CppFieldRef>(metadataHeader.fieldRefsOffset, metadataHeader.fieldRefsCount);
            }
            if (version > 20)
            {
                attributeTypeRanges = ReadMetadataClassArray<Il2CppCustomAttributeTypeRange>(metadataHeader.attributesInfoOffset, metadataHeader.attributesInfoCount);
                attributeTypes = ReadClassArray<int>(metadataHeader.attributeTypesOffset, metadataHeader.attributeTypesCount / 4);

                //windowsRuntimeTypeNamesOffsets = ReadClassArray<int>(metadataHeader.windowsRuntimeTypeNamesOffset, metadataHeader.windowsRuntimeTypeNamesSize / 4);
                //exportedTypeDefinitionsOffsets = ReadClassArray<int>(metadataHeader.exportedTypeDefinitionsOffset, metadataHeader.exportedTypeDefinitionsOffset / 4);
            }
            Dictionary<string, long> dic = new Dictionary<string, long>();
            foreach (var item in metadataHeader.GetType().GetFields())
            {
                if (item.Name.ToLower().Contains("offset"))
                {
                    switch (item.FieldType.Name)
                    {
                        case "Int32":
                            dic.Add(item.Name, (int)item.GetValue(metadataHeader));
                            break;
                        case "UInt32":
                            dic.Add(item.Name, (uint)item.GetValue(metadataHeader));
                            break;
                        case "Int16":
                            dic.Add(item.Name, (short)item.GetValue(metadataHeader));
                            break;
                        case "UInt16":
                            dic.Add(item.Name, (ushort)item.GetValue(metadataHeader));
                            break;
                        case "Byte":
                            dic.Add(item.Name, (byte)item.GetValue(metadataHeader));
                            break;
                        case "Int64" when is32Bit:
                            dic.Add(item.Name, (int)item.GetValue(metadataHeader));
                            break;
                        case "Int64":
                            dic.Add(item.Name, (long)item.GetValue(metadataHeader));
                            break;
                        case "UInt64" when is32Bit:
                            dic.Add(item.Name, (uint)item.GetValue(metadataHeader));
                            break;
                        case "UInt64":
                            dic.Add(item.Name, (long)item.GetValue(metadataHeader));
                            break;
                        default:
                            Console.WriteLine("error getting bytes");
                            break;
                    }
                }
            }
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(methodDefs[0]));
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(methodDefs[1]));
            //File.WriteAllText("strings2.json", Newtonsoft.Json.JsonConvert.SerializeObject(dic.OrderBy(o => o.Value), Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText("strings1.json", Newtonsoft.Json.JsonConvert.SerializeObject(metadataHeader, Newtonsoft.Json.Formatting.Indented));
            File.AppendAllText("strings1.json", BaseStream.Length.ToString());
            //foreach (var str in stringLiterals)

            bool one = false;

            //string ogstring = one ? "servers.php" : ".scpslgame.com/";
            //string newstring = one ? "serverlist" : ".southwoodstudios.com/";
            //string filename = one ? "global-metadata2.dat" : "global-metadata.dat";

            for (int i = 0; i < stringLiterals.Length; i++)
            {
                var str = stringLiterals[i];
                Position = (uint)(metadataHeader.stringLiteralDataOffset + str.dataIndex);
                string data = Encoding.UTF8.GetString(ReadBytes((int)str.length));
                //Debug.WriteLine(data);
                if (data.Equals(ogstring))
                {
                    Console.WriteLine("true");
                    //Position = (uint)(metadataHeader.stringLiteralDataOffset + str.dataIndex);
                    try
                    {
                        File.Delete(filename);
                    }
                    catch (Exception e)
                    {
                    }
                    //Position = (uint)(metadataHeader.stringLiteralDataOffset + str.dataIndex) + (uint)(newstring.Length - str.length);
                    UpdateOffsets(new MemoryStream(), (uint)(metadataHeader.stringLiteralDataOffset + str.dataIndex), (newstring.Length - data.Length), data, newstring);
                    File.WriteAllBytes(filename, ((MemoryStream)fsstream).ToArray());
                    break;
                }
            }
        }

        public void WriteClass<T>(Stream stream, ref T obj, long addedlen) where T : new()
        {
            var type = typeof(T);
            if (type.IsPrimitive)
            {
                object objk2 = obj; //long.Parse(obj.ToString()) + addedlen;
                switch (type.Name)
                {
                    case "Int32":
                        objk2 = int.Parse(obj.ToString()) + (int)addedlen;
                        break;
                    case "UInt32":
                        objk2 = uint.Parse(obj.ToString()) + (uint)addedlen;
                        break;
                    case "Int16":
                        objk2 = short.Parse(obj.ToString()) + (short)addedlen;
                        break;
                    case "UInt16":
                        objk2 = ushort.Parse(obj.ToString()) + (ushort)addedlen;
                        break;
                    case "Byte":
                        objk2 = byte.Parse(obj.ToString()) + (byte)addedlen;
                        break;
                    case "Int64" when is32Bit:
                        objk2 = int.Parse(obj.ToString()) + (int)addedlen;
                        break;
                    case "Int64":
                        objk2 = long.Parse(obj.ToString()) + (long)addedlen;
                        break;
                    case "UInt64" when is32Bit:
                        objk2 = uint.Parse(obj.ToString()) + (uint)addedlen;
                        break;
                    case "UInt64":
                        objk2 = ulong.Parse(obj.ToString()) + (ulong)addedlen;
                        break;
                    default:
                        break;
                }
                var bytes = GetBytes(obj);
                stream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                var t = obj;
                foreach (var i in t.GetType().GetFields())
                {
                    VersionAttribute versionAttribute = null;
                    {
                        if (Attribute.IsDefined(i, typeof(VersionAttribute)))
                        {
                            versionAttribute = (VersionAttribute)Attribute.GetCustomAttribute(i, typeof(VersionAttribute));
                        }
                    }
                    if (versionAttribute != null)
                    {
                        if (version < versionAttribute.Min || version > versionAttribute.Max)
                            continue;
                    }
                    switch (i.GetValue(obj).GetType().Name)
                    {
                        case "Int32":
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToInt32(i.GetValue(obj)) + (int)addedlen);
                            }
                            break;
                        case "UInt32":
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToUInt32(i.GetValue(obj)) + (uint)addedlen);
                            }
                            break;
                        case "Int16":
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToInt16(i.GetValue(obj)) + (short)addedlen);
                            }
                            break;
                        case "UInt16":
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToUInt16(i.GetValue(obj)) + (ushort)addedlen);
                            }
                            break;
                        case "Byte":
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToByte(i.GetValue(obj)) + (byte)addedlen);
                            }
                            break;
                        case "Int64" when is32Bit:
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToInt32(i.GetValue(obj)) + (int)addedlen);
                            }
                            break;
                        case "Int64":
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToInt64(i.GetValue(obj)) + (long)addedlen);
                            }
                            break;
                        case "UInt64" when is32Bit:
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToUInt32(i.GetValue(obj)) + (uint)addedlen);
                            }
                            break;
                        case "UInt64":
                            if (!Attribute.IsDefined(i, typeof(OffsetAttribute)))
                            {
                                break;
                            }
                            if (Convert.ToInt64(i.GetValue(obj)) >= (long)globaloffset)
                            {
                                i.SetValue(obj, Convert.ToUInt64(i.GetValue(obj)) + (ulong)addedlen);
                            }
                            break;
                        default:
                            break;
                    }
                    if (i.FieldType.IsPrimitive)
                    {
                        var bytes = GetBytes(i.GetValue(obj));
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        break;
                    }
                }
                obj = t;
            }
        }

        public void WriteClassArray<T>(long count, T[] obj) where T : new()
        {
            for (var i = 0; i < obj.Length; i++)
            {
                WriteClass<T>(fsstream, ref obj[i], currentDataOffset);
            }
        }

        public void WriteClassArray<T>(ref ulong addr, ref long count, T[] obj) where T : new()
        {
            fsstream.Position = (long)addr;
            //fsstream.Position = addr > globaloffset ? (long)addr + currentDataOffset : (long)addr;
            Position = addr;
            //objDbg(obj[0]);
            //addr = (ulong)fsstream.Position;
            WriteClassArray<T>(count, obj);
        }

        public void objDbg<T>(T obj)
        {
            foreach (var item in obj.GetType().GetFields())
            {
                switch (item.FieldType.Name)
                {
                    case "Int32":
                        Debug.WriteLineIf((int)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "UInt32":
                        Debug.WriteLineIf((uint)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "Int16":
                        Debug.WriteLineIf((short)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "UInt16":
                        Debug.WriteLineIf((ushort)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "Byte":
                        Debug.WriteLineIf((byte)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "Int64" when is32Bit:
                        Debug.WriteLineIf((int)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "Int64":
                        Debug.WriteLineIf((long)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "UInt64" when is32Bit:
                        Debug.WriteLineIf((uint)item.GetValue(obj) >= fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    case "UInt64":
                        Debug.WriteLineIf((ulong)item.GetValue(obj) >= (ulong)fsstream.Length, obj.GetType().Name + " addr + count " + item.Name);
                        break;
                    default:
                        break;
                }
            }
        }

        public void WriteClassArray<T>(ref uint addr, ref int count, T[] obj) where T : new()
        {
            fsstream.Position = (long)addr;
            //fsstream.Position = addr > globaloffset ? (long)addr + currentDataOffset : (long)addr;
            Position = addr;
            //for (int i = 0; i < obj.Length; i++)
                //objDbg(obj[i]);
            //addr = (uint)fsstream.Position;
            WriteClassArray<T>(count, obj);
        }

        private void WriteMetadataClassArray<T>(ref uint addr, ref int count, T[] obj) where T : new()
        {
            ulong ul = addr;
            long l = count;
            WriteClassArray<T>(ref ul, ref l /*/ MySizeOf(typeof(T))*/, obj);
            addr = (uint)ul;
            count = (int)l;
        }

        private void WriteBytesOffset(uint offset, int count)
        {
            Position = offset;
            var dat = ReadBytes(count);
            fsstream.Position = offset <= globaloffset ? offset : offset + currentDataOffset;
            fsstream.Write(dat, 0, dat.Length);
        }

        // stream
        // datapos - position start of changed string
        // dataaddedoffset - how much data has been added
        private void UpdateOffsets(MemoryStream stream, ulong datapos, int dataaddedoffset, string og, string newstr)
        {
            fsstream = stream;
            /*if (dataaddedoffset < 0)
            {
                globaloffset = datapos;
            }
            else
            {*/
                globaloffset = datapos + (ulong)dataaddedoffset;
            //}
            Console.WriteLine(datapos);
            Console.WriteLine("offset " + dataaddedoffset);
            //Position = 0;
            //currentDataOffset = (long)datapos + (long)dataaddedoffset;
            //currentDataOffset = 0;

            //Debug.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(metadataHeader, Newtonsoft.Json.Formatting.Indented));
            currentDataOffset = (long)dataaddedoffset;
            /*if (dataaddedoffset < 0)
            {
                currentDataOffset = 0;
            }*/
            if (version > 16)
            {
                FileData = "";
                List<byte> bytes = new List<byte>();
                long off = 0;
                for (int i = 0; i < stringLiterals.Length; i++)
                {
                    var str = stringLiterals[i];
                    var idx = metadataHeader.stringLiteralDataOffset + str.dataIndex;
                    if ((ulong)idx > datapos)
                    {
                        Position = (uint)(metadataHeader.stringLiteralDataOffset + str.dataIndex);
                        bytes.AddRange(ReadBytes((int)str.length));
                        stringLiterals[i].dataIndex += (int)dataaddedoffset;
                        //FileData += Encoding.UTF8.GetString(ReadBytes((int)str.length));
                        //Debug.WriteLine(stringLiterals[i].dataIndex);
                        //Debug.WriteLine(stringLiterals[i].dataIndex);
                    }
                    else if ((ulong)idx == datapos)
                    {
                        Debug.WriteLine("debugging");
                        stringLiterals[i].length = (uint)((int)stringLiterals[i].length + dataaddedoffset);
                        off = metadataHeader.stringLiteralDataOffset + str.dataIndex + stringLiterals[i].length;
                        bytes.AddRange(Encoding.UTF8.GetBytes(newstr));
                        //FileData += newstr;
                    }
                    else
                    {
                        Position = (uint)(metadataHeader.stringLiteralDataOffset + str.dataIndex);
                        bytes.AddRange(ReadBytes((int)str.length));
                        //FileData += Encoding.UTF8.GetString(ReadBytes((int)str.length));
                    }
                }
                var dat = Encoding.UTF8.GetBytes(FileData);
                Debug.WriteLine("byte count " + bytes.ToArray().Length);
                Debug.WriteLine("data count " + metadataHeader.stringLiteralDataCount);
                metadataHeader.stringLiteralDataCount = (int)bytes.ToArray().Length;
                Debug.WriteLine("equal " + ((ulong)off == datapos) + " off " + off + " datapos " + datapos);
                globaloffset = (ulong)off;
                //globaloffset = (ulong)off;//metadataHeader.stringLiteralDataOffset + metadataHeader.stringLiteralDataCount;
                fsstream.Position = metadataHeader.stringLiteralDataOffset;
                fsstream.Write(bytes.ToArray(), 0, bytes.ToArray().Length);
                FileData = "";
            }
            {
                Position = metadataHeader.stringOffset;
                //FileData = Encoding.UTF8.GetString(ReadBytes(metadataHeader.stringCount));
                var dat = ReadBytes(metadataHeader.stringCount);//Encoding.UTF8.GetBytes(FileData);
                fsstream.Position = metadataHeader.stringOffset <= globaloffset ? metadataHeader.stringOffset : metadataHeader.stringOffset + currentDataOffset;
                Debug.WriteLine("fsstream pos = " + fsstream.Position);
                Debug.WriteLine("string len = " + metadataHeader.stringCount);
                Debug.WriteLine("dat len = " + dat.Length);
                fsstream.Write(dat, 0, dat.Length);
                FileData = "";
            }
            WriteBytesOffset(metadataHeader.unresolvedVirtualCallParameterTypesOffset, metadataHeader.unresolvedVirtualCallParameterTypesCount);
            WriteBytesOffset(metadataHeader.unresolvedVirtualCallParameterRangesOffset, metadataHeader.unresolvedVirtualCallParameterRangesCount);
            WriteBytesOffset(metadataHeader.windowsRuntimeTypeNamesOffset, metadataHeader.exportedTypeDefinitionsCount);
            WriteBytesOffset(metadataHeader.exportedTypeDefinitionsOffset, metadataHeader.exportedTypeDefinitionsCount);
            WriteBytesOffset(metadataHeader.fieldAndParameterDefaultValueDataOffset, metadataHeader.fieldAndParameterDefaultValueDataCount);
            WriteBytesOffset(metadataHeader.fieldMarshaledSizesOffset, metadataHeader.fieldMarshaledSizesCount);
            WriteBytesOffset(metadataHeader.vtableMethodsOffset, metadataHeader.vtableMethodsCount);
            WriteBytesOffset(metadataHeader.interfaceOffsetsOffset, metadataHeader.interfaceOffsetsCount);
            WriteBytesOffset(metadataHeader.rgctxEntriesOffset, metadataHeader.rgctxEntriesCount);
            WriteBytesOffset(metadataHeader.referencedAssembliesOffset, metadataHeader.referencedAssembliesCount);
            WriteBytesOffset(metadataHeader.assembliesOffset, metadataHeader.assembliesCount);
            Position = 0;
            fsstream.Position = 0;
            WriteClass<Il2CppGlobalMetadataHeader>(fsstream, ref metadataHeader, currentDataOffset);
            /*{
                foreach (var metadataUsageList in metadataUsageLists)
                {
                    for (int i = 0; i < metadataUsageList.count; i++)
                    {
                        var offset = metadataUsageList.start + i;
                        var metadataUsagePair = metadataUsagePairs[offset];
                        var usage = GetEncodedIndexType(metadataUsagePair.encodedSourceIndex);
                        var decodedIndex = GetDecodedMethodIndex(metadataUsagePair.encodedSourceIndex);
                        //Debug.WriteLineIf(decodedIndex >= fsstream.Length, decodedIndex);
                    }
                }
            }*/
            //globaloffset = (ulong)currentDataOffset;
            //currentDataOffset = (long)dataaddedoffset;
            //Debug.WriteLine("offset " + metadataHeader.metadataUsagePairsOffset);
            //Debug.WriteLine("offset2 " + metadataHeader.metadataUsageListsOffset);
            //Debug.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(metadataHeader, Newtonsoft.Json.Formatting.Indented));
            //Debug.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(metadataHeader, Newtonsoft.Json.Formatting.Indented));
            /*foreach (var item in header.GetType().GetFields())
            {
                foreach (var item2 in metadataHeader.GetType().GetFields())
                {
                    if (item.Name.Equals(item2.Name) && !item.GetValue(header).Equals(item2.GetValue(metadataHeader)))
                    {
                        Debug.WriteLine(item.Name + " b4 = " + item.GetValue(header));
                        Debug.WriteLine(item2.Name + " af = " + item2.GetValue(metadataHeader));
                    }
                }
            }*/
            /*if (metadataHeader.stringLiteralOffset > globaloffset)
            {
                Debug.WriteLine("stringliteraloffset");
                metadataHeader.stringLiteralOffset += (uint)currentDataOffset;
            }*/


            //currentDataOffset = 0;
            //currentDataOffset = (long)dataaddedoffset;
            WriteMetadataClassArray<Il2CppImageDefinition>(ref metadataHeader.imagesOffset, ref metadataHeader.imagesCount, imageDefs);
            WriteMetadataClassArray<Il2CppTypeDefinition>(ref metadataHeader.typeDefinitionsOffset, ref metadataHeader.typeDefinitionsCount, typeDefs);
            WriteMetadataClassArray<Il2CppMethodDefinition>(ref metadataHeader.methodsOffset, ref metadataHeader.methodsCount, methodDefs);
            WriteMetadataClassArray<Il2CppParameterDefinition>(ref metadataHeader.parametersOffset, ref metadataHeader.parametersCount, parameterDefs);
            WriteMetadataClassArray<Il2CppFieldDefinition>(ref metadataHeader.fieldsOffset, ref metadataHeader.fieldsCount, fieldDefs);
            WriteMetadataClassArray<Il2CppFieldDefaultValue>(ref metadataHeader.fieldDefaultValuesOffset, ref metadataHeader.fieldDefaultValuesCount, fieldDefaultValues);
            WriteMetadataClassArray<Il2CppParameterDefaultValue>(ref metadataHeader.parameterDefaultValuesOffset, ref metadataHeader.parameterDefaultValuesCount, parameterDefaultValues);
            WriteMetadataClassArray<Il2CppPropertyDefinition>(ref metadataHeader.propertiesOffset, ref metadataHeader.propertiesCount, propertyDefs);


            //currentDataOffset = 0;

            var count = metadataHeader.interfacesCount / 4;
            WriteClassArray<int>(ref metadataHeader.interfacesOffset, ref count, interfaceIndices);

            count = metadataHeader.nestedTypesCount / 4;
            WriteClassArray<int>(ref metadataHeader.nestedTypesOffset, ref count, nestedTypeIndices);

            //currentDataOffset = (long)dataaddedoffset;


            WriteMetadataClassArray<Il2CppEventDefinition>(ref metadataHeader.eventsOffset, ref metadataHeader.eventsCount, eventDefs);
            WriteMetadataClassArray<Il2CppGenericContainer>(ref metadataHeader.genericContainersOffset, ref metadataHeader.genericContainersCount, genericContainers);
            WriteMetadataClassArray<Il2CppGenericParameter>(ref metadataHeader.genericParametersOffset, ref metadataHeader.genericParametersCount, genericParameters);


            count = metadataHeader.genericParameterConstraintsCount / 4;
            WriteClassArray<int>(ref metadataHeader.genericParameterConstraintsOffset, ref count, constraintIndices);

            if (version > 16)
            {
                WriteMetadataClassArray<Il2CppStringLiteral>(ref metadataHeader.stringLiteralOffset, ref metadataHeader.stringLiteralCount, stringLiterals);

                //currentDataOffset = 0;

                WriteMetadataClassArray<Il2CppMetadataUsageList>(ref metadataHeader.metadataUsageListsOffset, ref metadataHeader.metadataUsageListsCount, metadataUsageLists);
                //currentDataOffset = (long)dataaddedoffset;
                WriteMetadataClassArray<Il2CppMetadataUsagePair>(ref metadataHeader.metadataUsagePairsOffset, ref metadataHeader.metadataUsagePairsCount, metadataUsagePairs);


                WriteMetadataClassArray<Il2CppFieldRef>(ref metadataHeader.fieldRefsOffset, ref metadataHeader.fieldRefsCount, fieldRefs);
            }
            if (version > 20)
            {
                WriteMetadataClassArray<Il2CppCustomAttributeTypeRange>(ref metadataHeader.attributesInfoOffset, ref metadataHeader.attributesInfoCount, attributeTypeRanges);
                count = metadataHeader.attributeTypesCount / 4;
                WriteClassArray<int>(ref metadataHeader.attributeTypesOffset, ref count, attributeTypes);
            }
            //Debug.WriteLine(metadataUsageLists.Length);
            //Debug.WriteLine(metadataUsageLists[0].start);
            Debug.WriteLine(BaseStream.Length);
            Debug.WriteLine(fsstream.Length);
            {
                for (int i = 0; i < imageDefs.Length; i++)
                {
                    Debug.WriteLineIf(imageDefs[i].token >= fsstream.Length, imageDefs[i].token);
                }
            }
            /*Position = (ulong)fsstream.Length;
            var buf = ReadBytes((int)(BaseStream.Length - fsstream.Length));
            fsstream.Position = fsstream.Length;
            fsstream.Write(buf, 0, buf.Length);*/


            //File.WriteAllText("strings2.json", Newtonsoft.Json.JsonConvert.SerializeObject(metadataHeader, Newtonsoft.Json.Formatting.Indented));


            fsstream.Flush();
            fsstream.Close();
        }

        private void WriteClass(Stream stream, object classobj, ulong pos)
        {
            stream.Position = (long)pos;
            foreach (var i in classobj.GetType().GetFields())
            {
                VersionAttribute versionAttribute = null;
                if (Attribute.IsDefined(i, typeof(VersionAttribute)))
                {
                    versionAttribute = (VersionAttribute)Attribute.GetCustomAttribute(i, typeof(VersionAttribute));
                }
                if (versionAttribute != null)
                {
                    if (version < versionAttribute.Min || version > versionAttribute.Max)
                        continue;
                }
                var bytes = GetBytes(i.GetValue(classobj));
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private byte[] GetBytes(object obj)
        {
            var typename = obj.GetType().Name;
            switch (typename)
            {
                case "Int32":
                    return BitConverter.GetBytes((int)obj);
                case "UInt32":
                    return BitConverter.GetBytes((uint)obj);
                case "Int16":
                    return BitConverter.GetBytes((short)obj);
                case "UInt16":
                    return BitConverter.GetBytes((ushort)obj);
                case "Byte":
                    return BitConverter.GetBytes((byte)obj);
                case "Int64" when is32Bit:
                    return BitConverter.GetBytes((int)obj);
                case "Int64":
                    return BitConverter.GetBytes((long)obj);
                case "UInt64" when is32Bit:
                    return BitConverter.GetBytes((uint)obj);
                case "UInt64":
                    return BitConverter.GetBytes((ulong)obj);
                default:
                    Console.WriteLine("error getting bytes");
                    return null;
            }
        }

        private T[] ReadMetadataClassArray<T>(uint addr, int count) where T : new()
        {
            return ReadClassArray<T>(addr, count / MySizeOf(typeof(T)));
        }

        public Il2CppFieldDefaultValue GetFieldDefaultValueFromIndex(int index)
        {
            return fieldDefaultValues.FirstOrDefault(x => x.fieldIndex == index);
        }

        public Il2CppParameterDefaultValue GetParameterDefaultValueFromIndex(int index)
        {
            return parameterDefaultValues.FirstOrDefault(x => x.parameterIndex == index);
        }

        public uint GetDefaultValueFromIndex(int index)
        {
            return (uint)(metadataHeader.fieldAndParameterDefaultValueDataOffset + index);
        }

        public string GetStringFromIndex(uint index)
        {
            return ReadStringToNull(metadataHeader.stringOffset + index);
        }

        public int GetCustomAttributeIndex(Il2CppImageDefinition imageDef, int customAttributeIndex, uint token)
        {
            if (version > 24)
            {
                var end = imageDef.customAttributeStart + imageDef.customAttributeCount;
                for (int i = imageDef.customAttributeStart; i < end; i++)
                {
                    if (attributeTypeRanges[i].token == token)
                    {
                        return i;
                    }
                }
                return -1;
            }
            else
            {
                return customAttributeIndex;
            }
        }

        public string GetStringLiteralFromIndex(uint index)
        {
            var stringLiteral = stringLiterals[index];
            Position = (uint)(metadataHeader.stringLiteralDataOffset + stringLiteral.dataIndex);
            return Encoding.UTF8.GetString(ReadBytes((int)stringLiteral.length));
        }

        private void ProcessingMetadataUsage()
        {
            metadataUsageDic = new Dictionary<uint, SortedDictionary<uint, uint>>();
            for (uint i = 1; i <= 6u; i++)
            {
                metadataUsageDic[i] = new SortedDictionary<uint, uint>();
            }
            foreach (var metadataUsageList in metadataUsageLists)
            {
                for (int i = 0; i < metadataUsageList.count; i++)
                {
                    var offset = metadataUsageList.start + i;
                    /*if (offset == 0)
                    {
                        Debug.WriteLine(offset);
                    }*/
                    //Debug.WriteLine(offset);
                    var metadataUsagePair = metadataUsagePairs[offset];
                    var usage = GetEncodedIndexType(metadataUsagePair.encodedSourceIndex);
                    var decodedIndex = GetDecodedMethodIndex(metadataUsagePair.encodedSourceIndex);
                    metadataUsageDic[usage][metadataUsagePair.destinationIndex] = decodedIndex;
                }
            }
            maxMetadataUsages = metadataUsageDic.Max(x => x.Value.Max(y => y.Key)) + 1;
        }

        private uint GetEncodedIndexType(uint index)
        {
            return (index & 0xE0000000) >> 29;
        }

        private uint GetRevIdxType(uint index)
        {
            return (index & 0xE0000000U) >> 29;
        }

        private uint GetDecodedMethodIndex(uint index)
        {
            return index & 0x1FFFFFFFU;
        }

        private int MySizeOf(Type type)
        {
            var size = 0;
            foreach (var i in type.GetFields())
            {
                var attr = (VersionAttribute)Attribute.GetCustomAttribute(i, typeof(VersionAttribute));
                if (attr != null)
                {
                    if (version < attr.Min || version > attr.Max)
                        continue;
                }
                switch (i.FieldType.Name)
                {
                    case "Int32":
                    case "UInt32":
                        size += 4;
                        break;
                    case "Int16":
                    case "UInt16":
                        size += 2;
                        break;
                }
            }
            return size;
        }
    }
}
