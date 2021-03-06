/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using Dolittle.Logging;
using Dolittle.TimeSeries.Modules.Connectors;

namespace Dolittle.TimeSeries.Modbus
{
    /// <summary>
    /// Represents a <see cref="IAmAPullConnector">pull connector</see> for Modbus
    /// </summary>
    public class Connector : IAmAPullConnector
    {
        readonly RegistersConfiguration _registers;
        readonly ConnectorConfiguration _configuration;
        readonly IMaster _master;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="Connector"/>
        /// </summary>
        /// <param name="registers">The <see cref="RegistersConfiguration">configured registers</see></param>
        /// <param name="configuration"><see cref="ConnectorConfiguration">Configuration</see></param>
        /// <param name="master">The <see cref="IMaster"/></param>
        /// <param name="logger"><see cref="ILogger"/> for logging</param>
        public Connector(
            RegistersConfiguration registers,
            ConnectorConfiguration configuration,
            IMaster master,
            ILogger logger)
        {
            _registers = registers;
            _configuration = configuration;
            _logger = logger;
            _master = master;
        }


        /// <inheritdoc/>
        public Source Name => "Modbus";

        /// <inheritdoc/>

        public IEnumerable<TagWithData> GetAllData()
        {
            var data = new List<TagWithData>();

            var swapWords = _configuration.Endianness.ShouldSwapWords();

            foreach (var register in _registers)
            {
                _master.Read(register).ContinueWith(result =>
                {
                    var bytes = result.Result;
                    var byteSize = GetByteSizeFrom(register.DataType);

                    if (swapWords)
                    {
                        var tempBytes = new List<byte>();
                        for (var byteIndex = bytes.Length; byteIndex >= 0; byteIndex -= byteSize)
                        {
                            tempBytes.AddRange(bytes.Skip(byteIndex).Take(byteSize).ToArray());
                        }
                        bytes = tempBytes.ToArray();
                    }
                    for (var byteIndex = 0; byteIndex < bytes.Length; byteIndex += byteSize)
                    {
                        var tag = $"{register.Unit}:{register.StartingAddress + byteIndex / (byteSize / 2)}";
                        var byteBatch = bytes.Skip(byteIndex).Take(byteSize).ToArray();
                        var payload = ConvertBytes(register.DataType, byteBatch);
                        data.Add(new TagWithData(tag, payload));
                        _logger.Information($"Tag: {tag}, Value : {payload}");
                    }

                }).Wait();
            }
            return data;
        }

        /// <inheritdoc/>
        public object GetData(Tag tag)
        {
            return new Measurement<Int32> { Value = 0 };
        }
        ushort GetByteSizeFrom(DataType type)
        {
            switch (type)
            {
                case DataType.Int32:
                    return 4;
                case DataType.Uint32:
                    return 4;
                case DataType.Float:
                    return 4;
            }
            return 2;
        }

        object ConvertBytes(DataType type, byte[] bytes)
        {
            switch (type)
            {
                case DataType.Int32:
                    return BitConverter.ToInt32(bytes);
                case DataType.Uint32:
                    return BitConverter.ToUInt32(bytes);
                case DataType.Float:
                    return BitConverter.ToSingle(bytes);
                case DataType.Int16:
                    return BitConverter.ToInt16(bytes);
            }
            return 0;
        }
    }
}