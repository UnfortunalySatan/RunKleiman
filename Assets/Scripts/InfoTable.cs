using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using YG;
using Unity.VisualScripting.AssemblyQualifiedNameParser;

public class InfoTable : MonoBehaviour
{
    [SerializeField] private TMP_Text nickname;
    [SerializeField] private GameObject playerAvatar;
    [SerializeField] private TMP_Text playerBestScore;
    [SerializeField] private GameObject alert;

    private void OnEnable()
    {
        YG2.onGetSDKData += UpdateUserData;

        if (YG2.isSDKEnabled)
        {
            UpdateUserData(); 
        }
    }
    private void OnDisable()
    {
        YG2.onGetSDKData -= UpdateUserData;
    }
    private void UpdateUserData()
    {
        playerBestScore.text = YG2.saves.bestRunScore.ToString();
        Debug.Log(YG2.saves.bestRunScore);
        if (YG2.player.auth)
        {
            alert.SetActive(false);
            nickname.text = YG2.player.name;

            if (!string.IsNullOrEmpty(YG2.player.photo))
            {
                StartCoroutine(LoadImage(YG2.player.photo));
            }
        }
        else
        {
            alert.SetActive(true);
        }
    }
    IEnumerator LoadImage(string mediaUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(mediaUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            // Создаем Sprite из скачанной Texture2D
            Sprite avatarSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            // Присваиваем спрайт компоненту Image
            playerAvatar.GetComponent<Image>().sprite = avatarSprite;
        }
        else
        {
            Debug.LogError("Ошибка загрузки аватарки: " + request.error);
        }
    }

    public void GoAuthorize()
    {
        if (!YG2.player.auth)
        {
            YG2.OpenAuthDialog();
        }
    }
}
