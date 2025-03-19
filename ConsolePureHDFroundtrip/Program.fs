open System
open System.Collections.Generic
open System.IO
open System.Runtime.InteropServices
open System.Diagnostics
open PureHDF
open PureHDF.Selections
open PureHDF.Filters
open PureHDF.VOL.Native

let datasetName = "peaks"

[<Struct; StructLayout(LayoutKind.Sequential, Pack = 1)>]
type Peak =
    {
        accession : string
        mz : double
        ic : single
    }

let r = new Random(123)

let dim1Size = 100
let dims = [| uint64 dim1Size |]

let peaks =
    Array.init dim1Size
        (fun i ->
            { accession = i.ToString(); mz = r.NextDouble(); ic = r.NextSingle() }
        )


let saveDataSet (file : FileInfo) =
    let dataset = H5Dataset<Peak[]>(peaks)

    let hdfFile = H5File()
    hdfFile[datasetName] <- dataset

    let stream = file.Create()
    let writer = hdfFile.BeginWrite(stream, H5WriteOptions(IncludeStructProperties=true))
    writer.Write(dataset, peaks)

    writer.Dispose()
    stream.Close()

let readDataset (file : FileInfo) =
    let readDataset = H5File.Open(file.OpenRead()).Dataset(datasetName)
    readDataset.Read<Peak[]>()

let checkData readBack =
    (peaks, readBack)
    ||> Array.iter2 (
        fun expected actual ->
            if expected.mz <> actual.mz then failwithf "mz mismatch: %f vs %f" expected.mz actual.mz
            if expected.ic <> actual.ic then failwithf "ic mismatch: %f vs %f" expected.ic actual.ic
            if expected.accession <> actual.accession then failwithf "accession mismatch: %s vs %s" expected.accession actual.accession
    )

let baseFilename = @"c:\tmp\roundtrip"
let rawFile = System.IO.FileInfo(baseFilename + ".h5")

saveDataSet rawFile
let rereadRaw = readDataset rawFile
checkData rereadRaw
