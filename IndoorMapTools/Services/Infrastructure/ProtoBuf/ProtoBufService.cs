using IndoorMapTools.Model;
using IndoorMapTools.Services.Application;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace IndoorMapTools.Services.Infrastructure.ProtoBuf
{
    /// <summary>
    /// 프로젝트 데이터를 바이너리 형식으로 저장하고 로드하는 기능을 제공
    /// </summary>
    public class ProtoBufService : IProjectPersistenceService
    {
        private readonly RuntimeTypeModel protoBufModel;

        // 정의된 모델 설정에 따라 ProtoBufModel을 초기화
        public ProtoBufService()
        {
            protoBufModel = RuntimeTypeModel.Create();

            // MEMBERS_DEFINITIONS 에 따라 필드 추가
            foreach(var (type, members) in ProtoBufDefinitions.MEMBERS_DEFINITIONS)
            {
                var metadata = protoBufModel.Add(type, false);
                for(int i = 0; i < members.Length; i++)
                {
                    if(members[i] != null && members[i].Length > 0)
                    {
                        if(members[i][0] == '&') // 참조 필드 처리
                        {
                            if(members[i].Length < 2) continue;
                            var vMember = metadata.AddField(i + 1, members[i].Substring(1));
                            vMember.AsReference = true;
                        }
                        else metadata.AddField(i + 1, members[i]);
                    }
                }
            }

            // SURROGATE_DEFINITIONS 에 따라 서로게이트 추가
            foreach(var surrogate in ProtoBufDefinitions.SURROGATE_DEFINITIONS)
                protoBufModel.Add(surrogate.Key, false).SetSurrogate(surrogate.Value);
        }

        /// <summary>
        /// 주어진 파일 경로에서 프로젝트 데이터를 로드
        /// </summary>
        /// <param name="filePath">프로젝트 파일 경로</param>
        /// <returns>로드된 프로젝트 객체</returns>
        public Project LoadProject(string filePath)
        {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (Project)protoBufModel.Deserialize(fs, null, typeof(Project));
        }


        /// <summary>
        /// 주어진 파일 경로에 프로젝트 데이터를 저장
        /// 지정된 경로에 파일이 이미 존재할 경우 삭제 후 저장
        /// </summary>
        /// <param name="project">저장할 프로젝트 객체</param>
        /// <param name="filePath">저장할 파일 경로</param>
        public void SaveProject(Project project, string filePath)
        {
            using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            protoBufModel.Serialize(fs, project);
        }
    }
}
