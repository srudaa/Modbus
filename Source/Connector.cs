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
        readonly IMaster _master;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="Connector"/>
        /// </summary>
        /// <param name="registers">The <see cref="RegistersConfiguration">configured registers</see></param>
        /// <param name="master">The <see cref="IMaster"/></param>
        /// <param name="logger"><see cref="ILogger"/> for logging</param>
        public Connector(
            RegistersConfiguration registers,
            IMaster master,
            ILogger logger)
        {
            _registers = registers;
            _logger = logger;
            _master = master;
        }


        /// <inheritdoc/>
        public Source Name => "Modbus";

        /// <inheritdoc/>

        public IEnumerable<TagWithData> GetAllData()
        {
            var data = new List<TagWithData>();

            foreach (var register in _registers)
            {
                var bytes = _master.Read(register);

                var bytesize = GetByteSizeFrom(register.DataType);

                for (int i = 0; i < bytes.Length; i += bytesize)
                {
                    var tag = $"{register.Unit}:{i / (bytesize/2) + (bytesize/2)}";
                    byte[] bytebatch = bytes.Skip(i).Take(bytesize).ToArray();
                    var payload = ConvertBytes(register.DataType, bytebatch);
                    data.Add(new TagWithData(tag, payload));
                    _logger.Information($"Tag: {tag}, Value : {payload}");
                }
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
            }
            return 0;
        }
    }
}