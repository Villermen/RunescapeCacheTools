﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Villermen.RuneScapeCacheTools.Cache.RuneTek5;
using Villermen.RuneScapeCacheTools.Exceptions;
using Villermen.RuneScapeCacheTools.Extensions;

namespace Villermen.RuneScapeCacheTools.Cache.FileTypes
{
    /// <summary>
    /// A cache file that contains multiple files.
    /// </summary>
    public class EntryFile : CacheFile
    {
        private readonly Dictionary<int, BinaryFile> _entries = new Dictionary<int, BinaryFile>();

        private int _capacity = 0;
        
        /// <summary>
        /// The amount of entries that can be stored in this file.
        /// </summary>
        public int Capacity
        {
            get { return this._capacity; }
            set
            {
                var highestIndex = this._entries.Keys.DefaultIfEmpty().Max();
                if (value <= highestIndex)
                {
                    throw new ArgumentOutOfRangeException($"Can not set entry file's capacity to {value} as there are entries up to index {highestIndex}.");
                }

                this._capacity = value;
            }
        }

        public bool Empty => !this._entries.Any();

        public T GetEntry<T>(int entryId) where T : CacheFile
        {
            var binaryFile = this._entries[entryId];
            
            if (typeof(T) == typeof(BinaryFile))
            {
                return binaryFile as T;
            }

            var file = Activator.CreateInstance<T>();
            file.FromBinaryFile(binaryFile);

            return file;
        }

        public IEnumerable<T> GetEntries<T>() where T : CacheFile
        {
            return this._entries.Keys
                .OrderBy(entryId => entryId)
                .Select(this.GetEntry<T>);
        }

        /// <summary>
        /// </summary>
        /// <param name="entryId">The EntryFile's capacity will be increased to match the given index.</param>
        /// <param name="entry"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void AddEntry(int entryId, CacheFile entry)
        {
            var binaryFileEntry = entry.ToBinaryFile();
            
            // Only store entries that are not "empty" (a 0-byte only)
            if (!binaryFileEntry.Data.SequenceEqual(new byte[] {0}))
            {
                if (binaryFileEntry.Info == null)
                {
                    binaryFileEntry.Info = new CacheFileInfo();
                }

                binaryFileEntry.Info.Index = this.Info.Index;
                binaryFileEntry.Info.FileId = this.Info.FileId;
                binaryFileEntry.Info.EntryId = entryId;

                this._entries.Add(entryId, binaryFileEntry);
            }

            // Increase capacity, even for empty entries so that they will be written out as well
            if (entryId >= this.Capacity)
            {
                this.Capacity = entryId + 1;
            }
        }

        public void AddEntry(int entryId, byte[] entryData)
        {
            this.AddEntry(entryId, new BinaryFile
            {
                Data = entryData
            });
        }

        public override void Decode(byte[] data)
        {
            /*
             * Format visualization:
             * chunk1 data:                      [entry1chunk1][entry2chunk1]
             * chunk2 data:                      [entry1chunk2][entry2chunk2]
             * delta-encoded chunk1 entry sizes: [entry1chunk1size][entry2chunk1size]
             * delta-encoded chunk2 entry sizes: [entry1chunk2size][entry2chunk2size]
             *                                   [chunkamount (2)]
             *
             * Add entry1chunk2 to entry1chunk1 and voilà, unnecessarily complex bullshit solved.
             */

            var entriesData = new byte[this.Info.Entries.Length][];

            var reader = new BinaryReader(new MemoryStream(data));

            reader.BaseStream.Position = reader.BaseStream.Length - 1;
            var amountOfChunks = reader.ReadByte();

            if (amountOfChunks == 0)
            {
                throw new DecodeException("Entry file contains no chunks.");
            }

            // Read the sizes of the child entries and individual chunks
            var chunkEntrySizes = new int[amountOfChunks, this.Info.Entries.Length];

            reader.BaseStream.Position = reader.BaseStream.Length - 1 - amountOfChunks * this.Info.Entries.Length * 4;

            for (var chunkId = 0; chunkId < amountOfChunks; chunkId++)
            {
                var chunkSize = 0;
                for (var entryId = 0; entryId < this.Info.Entries.Length; entryId++)
                {
                    // Read the delta encoded chunk length
                    var delta = reader.ReadInt32BigEndian();
                    chunkSize += delta;

                    // Store the size of this entry in this chunk
                    chunkEntrySizes[chunkId, entryId] = chunkSize;
                }
            }

            // Read the data
            reader.BaseStream.Position = 0;
            for (var chunkId = 0; chunkId < amountOfChunks; chunkId++)
            {
                for (var entryId = 0; entryId < this.Info.Entries.Length; entryId++)
                {
                    // Read the bytes of the entry into the archive entries
                    var entrySize = chunkEntrySizes[chunkId, entryId];
                    var entryData = reader.ReadBytes(entrySize);

                    if (entryData.Length != entrySize)
                    {
                        throw new EndOfStreamException("End of file reached while reading the archive.");
                    }

                    // Put or append the entry data to the result
                    entriesData[entryId] = chunkId == 0 ? entryData : entriesData[entryId].Concat(entryData).ToArray();
                }
            }
            
            // Convert to binary files and store
            for(var entryId = 0; entryId < entriesData.Length; entryId++)
            {
                this.AddEntry(entryId, entriesData[entryId]);
            }
        }

        public override byte[] Encode()
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);

            for (var entryId = 0; entryId < this.Capacity; entryId++)
            {
                // Write empty entries as a single 0 byte
                writer.Write(
                    this._entries.ContainsKey(entryId) 
                        ? this._entries[entryId].Data 
                        : new byte[] {0});
            }

            // TODO: Split entries into multiple chunks (when?)
            byte amountOfChunks = 1;

            for (var chunkId = 0; chunkId < amountOfChunks; chunkId++)
            {
                // Write delta encoded entry sizes
                var previousEntrySize = 0;
                for (var entryId = 0; entryId < this.Capacity; entryId++)
                {
                    var entrySize = this._entries.ContainsKey(entryId)
                        ? this._entries[entryId].Data.Length
                        : 1;

                    var delta = entrySize - previousEntrySize;

                    writer.WriteInt32BigEndian(delta);

                    previousEntrySize = entrySize;
                }
            }

            // Finish of with the amount of chunks
            writer.Write(amountOfChunks);

            return memoryStream.ToArray();
        }
    }
}
