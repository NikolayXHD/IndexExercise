using System.Runtime.Serialization;

namespace IndexExercise.Index.Demo
{
	[DataContract(Name = "Index")]
	public class IndexConfig
	{
		[DataMember(Name = "FileNameRegex")]
		public string FileNameRegex { get; set; }

		[DataMember(Name = "IndexDirectory")]
		public string IndexDirectory { get; set; }

		[DataMember(Name = "Directory")]
		public DirectoryConfig[] Directories { get; set; }

		[DataMember(Name = "File")]
		public FileConfig[] Files { get; set; }

		[DataMember(Name = "MaxFileLength")]
		public long? MaxFileLength {get; set; }

		[DataMember(Name = "MaxReadAttempts")]
		public int? MaxReadAttempts {get; set; }
		
		[DataMember(Name = "AdditionalWordChars")]
		public string AdditionalWordChars { get; set; }

		[DataMember(Name = "MaxWordLength")]
		public int? MaxWordLength { get; set; }

		[DataMember(Name = "CaseSensitive")]
		public bool? CaseSensitive { get; set; }
	}
}