using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Sent from server to client.
/// </summary>
public enum ServerPackets
{
    welcome = 1
}

/// <summary>
/// Sent from client to server.
/// </summary>
public enum ClientPackets
{
    welcomeReceived = 1
}

public class Packet : IDisposable
{
    private List<byte> buffer;
    private byte[] readableBuffer;
    private int readPos;

    /// <summary>
    /// Creates a new empty packet (without an ID).
    /// </summary>
    public Packet()
    {
        // Initialize buffer
        buffer = new List<byte>();
        readPos = 0;
    }


    /// <summary>
    /// Creates a new packet with a given ID.
    /// Used for sending.
    /// </summary>
    public Packet(int _id)
    {
        // Initialize buffer
        buffer = new List<byte>();
        readPos = 0;

        // Write packet id to the buffer
        Write(_id);
    }


    /// <summary>
    /// Creates a packet from which data can be read.
    /// Used for receiving.
    /// </summary>
    public Packet(byte[] _data)
    {
        // Initialize buffer
        buffer = new List<byte>();
        readPos = 0;

        SetBytes(_data);
    }

    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-region
    // #region allows expanding or collapsing when using the
    // outlining feature of the code editor.
    #region Functions
    /// <summary>
    /// Sets the packet's content and prepares it to be read.
    /// </summary>
    /// <param name="_data">
    /// The bytes to add to the packet.
    /// </param>
    public void SetBytes(byte[] _data)
    {
        Write(_data);
        readableBuffer = buffer.ToArray();
    }


    /// <summary>
    /// Inserts the length of the packet's content
    /// at the start of the buffer.
    /// </summary>
    public void WriteLength()
    {
        // Insert the byte length of the packet at the very beginning
        buffer.InsertRange(
            0,
            BitConverter.GetBytes(
                buffer.Count));
    }


    /// <summary>Inserts the given int at the start of the buffer.</summary>
    /// <param name="_value">The int to insert.</param>
    public void InsertInt(int _value)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(_value)); // Insert the int at the start of the buffer
    }

    /// <summary>Gets the packet's content in array form.</summary>
    public byte[] ToArray()
    {
        readableBuffer = buffer.ToArray();
        return readableBuffer;
    }

    /// <summary>Gets the length of the packet's content.</summary>
    public int Length()
    {
        return buffer.Count; // Return the length of buffer
    }

    /// <summary>Gets the length of the unread data contained in the packet.</summary>
    public int UnreadLength()
    {
        return Length() - readPos; // Return the remaining length (unread)
    }

    /// <summary>Resets the packet instance to allow it to be reused.</summary>
    /// <param name="_shouldReset">Whether or not to reset the packet.</param>
    public void Reset(bool _shouldReset = true)
    {
        if (_shouldReset)
        {
            buffer.Clear(); // Clear buffer
            readableBuffer = null;
            readPos = 0; // Reset readPos
        }
        else
        {
            readPos -= 4; // "Unread" the last read int
        }
    }
    #endregion

    #region Write Data
    /// <summary>Adds a byte to the packet.</summary>
    /// <param name="_value">The byte to add.</param>
    public void Write(byte _value)
    {
        buffer.Add(_value);
    }
    /// <summary>Adds an array of bytes to the packet.</summary>
    /// <param name="_value">The byte array to add.</param>
    public void Write(byte[] _value)
    {
        buffer.AddRange(_value);
    }
    /// <summary>Adds a short to the packet.</summary>
    /// <param name="_value">The short to add.</param>
    public void Write(short _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds an int to the packet.</summary>
    /// <param name="_value">The int to add.</param>
    public void Write(int _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a long to the packet.</summary>
    /// <param name="_value">The long to add.</param>
    public void Write(long _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a float to the packet.</summary>
    /// <param name="_value">The float to add.</param>
    public void Write(float _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a bool to the packet.</summary>
    /// <param name="_value">The bool to add.</param>
    public void Write(bool _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a string to the packet.</summary>
    /// <param name="_value">The string to add.</param>
    public void Write(string _value)
    {
        Write(_value.Length); // Add the length of the string to the packet
        buffer.AddRange(Encoding.ASCII.GetBytes(_value)); // Add the string itself
    }
    #endregion

    #region Read Data
    /// <summary>
    /// Reads a byte from the packet.
    /// </summary>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public byte ReadByte(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            // Get the byte at readPos' position
            byte _value = readableBuffer[readPos];
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                // Increase readPos by 1
                readPos += 1;
            }
            // Return the byte
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'byte'!");
        }
    }

    /// <summary>
    /// Reads an array of bytes from the packet.
    /// </summary>
    /// <param name="_length">
    /// The length of the byte array.
    /// </param>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public byte[] ReadBytes(
        int _length,
        bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            // Get the bytes at readPos' position with a range of _length
            byte[] _value = buffer.GetRange(
                readPos,
                _length).ToArray();
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                // Increase readPos by _length
                readPos += _length;
            }
            // Return the bytes
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'byte[]'!");
        }
    }

    /// <summary>
    /// Reads a short from the packet.
    /// </summary>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public short ReadShort(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            // Convert the bytes to a short
            short _value = BitConverter.ToInt16(
                readableBuffer,
                readPos);
            if (_moveReadPos)
            {
                // If _moveReadPos is true and there are unread bytes
                // Increase readPos by 2
                readPos += 2;
            }
            // Return the short
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'short'!");
        }
    }

    /// <summary>
    /// Reads an int from the packet.
    /// </summary>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public int ReadInt(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            // Convert the bytes to an int
            int _value = BitConverter.ToInt32(readableBuffer, readPos);
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                // Increase readPos by 4
                readPos += 4;
            }
            // Return the int
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'int'!");
        }
    }

    /// <summary>
    /// Reads a long from the packet.
    /// </summary>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public long ReadLong(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            // Convert the bytes to a long
            long _value = BitConverter.ToInt64(readableBuffer, readPos);
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                // Increase readPos by 8
                readPos += 8;
            }
            // Return the long
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'long'!");
        }
    }

    /// <summary>
    /// Reads a float from the packet.
    /// </summary>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public float ReadFloat(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            // Convert the bytes to a float
            float _value = BitConverter.ToSingle(readableBuffer, readPos);
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                // Increase readPos by 4
                readPos += 4;
            }
            // Return the float
            return _value;
        }
        else
        {
            throw new Exception("Could not read value of type 'float'!");
        }
    }

    /// <summary>
    /// Reads a bool from the packet.
    /// </summary>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public bool ReadBool(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            // Convert the bytes to a bool
            bool _value =
                    BitConverter.ToBoolean(
                        readableBuffer,
                        readPos);
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += 1; // Increase readPos by 1
            }
            return _value; // Return the bool
        }
        else
        {
            throw new Exception("Could not read value of type 'bool'!");
        }
    }

    /// <summary>
    /// Reads a string from the packet.
    /// </summary>
    /// <param name="_moveReadPos">
    /// Whether or not to move the buffer's read position.
    /// </param>
    public string ReadString(bool _moveReadPos = true)
    {
        try
        {
            // Get the length of the string
            int _length = ReadInt();
            // Convert the bytes to a string
            string _value = Encoding.ASCII.GetString(
                readableBuffer,
                readPos,
                _length);
            if (_moveReadPos
                && _value.Length > 0)
            {
                // If _moveReadPos is true string is not empty
                // Increase readPos by the length of the string
                readPos += _length;
            }
            return _value; // Return the string
        }
        catch
        {
            throw new Exception("Could not read value of type 'string'!");
        }
    }
    #endregion

    private bool disposed = false;

    protected virtual void Dispose(bool _disposing)
    {
        if (!disposed)
        {
            if (_disposing)
            {
                buffer = null;
                readableBuffer = null;
                readPos = 0;
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        // https://docs.microsoft.com/en-us/dotnet/api/system.gc.suppressfinalize?view=netcore-3.1
        // Request that the common language runtime NOT to call the
        // finalizer for the specified object (this).
        // Prevent a redundant garbage collection from being called.
        GC.SuppressFinalize(this);
    }
}
