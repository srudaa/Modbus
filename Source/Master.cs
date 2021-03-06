/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dolittle.Collections;
using Dolittle.Lifecycle;
using Dolittle.Logging;
using NModbus;
using NModbus.IO;

namespace Dolittle.TimeSeries.Modbus
{
    /// <summary>
    /// Represents an implementation of <see cref="IMaster"/>
    /// </summary>
    [Singleton]
    public class Master : IMaster, IDisposable
    {
        readonly ConnectorConfiguration _configuration;
        readonly ILogger _logger;

        TcpClient _client;
        TcpClientAdapter _adapter;
        IModbusMaster _master;


        /// <summary>
        /// Initializes a new instance of <see cref="Master"/>
        /// </summary>
        /// <param name="configuration"><see cref="ConnectorConfiguration">Configuration</see></param>
        /// <param name="logger"><see cref="ILogger"/> to use for logging</param>
        public Master(ConnectorConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client?.Dispose();
            _client = null;
            _adapter?.Dispose();
            _adapter = null;
            _master?.Dispose();
            _master = null;
        }

        /// <inheritdoc/>
        public async Task<byte[]> Read(Register register)
        {
            MakeSureClientIsConnected();

            _logger.Information($"Getting data from slave {register.Unit} startingAdress {register.StartingAddress} size {register.Size} as DataType {Enum.GetName(typeof(DataType), register.DataType)}");

            ushort[] result;

            var size = Convert.ToUInt16(register.Size * GetDataSizeFrom(register.DataType));

            try
            {
                switch (register.FunctionCode)
                {
                    case FunctionCode.HoldingRegister:
                        result = await _master.ReadHoldingRegistersAsync(register.Unit, register.StartingAddress, size);
                        break;
                    case FunctionCode.InputRegister:
                        result = await _master.ReadInputRegistersAsync(register.Unit, register.StartingAddress, size);
                        break;
                    default:
                        result = new ushort[0];
                        break;
                }
                var bytes = result.GetBytes(_configuration.Endianness);
                return bytes;

            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Trouble reading register {register}");
                return null;
            }
        }

        ushort GetDataSizeFrom(DataType type)
        {
            switch (type)
            {
                case DataType.Int32:
                    return 2;
                case DataType.Uint32:
                    return 2;
                case DataType.Float:
                    return 2;
            }
            return 1;
        }

        void MakeSureClientIsConnected()
        {
            if (_client != null && !_client.Connected)
            {
                _client.Dispose();
                _client = null;
                _adapter.Dispose();
                _adapter = null;
                _master.Dispose();
                _master = null;
            }

            if (_client == null)
            {
                _client = new TcpClient(_configuration.Ip, _configuration.Port);
                _adapter = new TcpClientAdapter(_client);
                var factory = new ModbusFactory();
                if (_configuration.UseASCII) _master = factory.CreateAsciiMaster(_adapter);
                else if (_configuration.Protocol == Protocol.Tcp) _master = factory.CreateMaster(_client);
                else _master = factory.CreateRtuMaster(_adapter);
            }
        }
    }
}