using System;
using System.Collections.Generic;
using UnityEngine;

namespace StratusFieldLink.Model
{
    /// <summary>
    /// Wire models. Field names are lower-camel on purpose: JsonUtility matches
    /// field names verbatim and the API serialises camelCase. Do not rename these
    /// without changing the server contract.
    /// </summary>
    [Serializable]
    public class SketchNodeDto
    {
        public float x;
        public float y;
        public float z;

        public SketchNodeDto()
        {
        }

        public SketchNodeDto(Vector3 point)
        {
            x = point.x;
            y = point.y;
            z = point.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class FieldSketchDto
    {
        public string id;
        public string packageId;
        public string crewId;
        public string discipline = "Piping";
        public float diameterInches = 4f;
        public string material = "Carbon Steel";
        public List<SketchNodeDto> nodes = new List<SketchNodeDto>();
        public string capturedAtUtc;
        public float clientLengthFeet;
        public string clientVersion;

        public static FieldSketchDto NewDraft(string packageId, string crewId)
        {
            return new FieldSketchDto
            {
                id = "SK-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant(),
                packageId = packageId,
                crewId = crewId,
                capturedAtUtc = DateTime.UtcNow.ToString("o"),
                clientVersion = Config.FieldLinkConfig.ClientVersion,
                nodes = new List<SketchNodeDto>()
            };
        }
    }

    /// <summary>Shape the API returns on a 4xx. Nothing parses this yet.</summary>
    [Serializable]
    public class ApiErrorDto
    {
        public string error;
    }
}
