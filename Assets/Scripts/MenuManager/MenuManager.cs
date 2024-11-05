using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class MenuManager : MonoBehaviour
{
    public InputField UserName, Email, Password;
    public Text ErrorMessage;
    public string SceneName = "";

    void Start()
    {
        ErrorMessage.text = "";
    }

    public void RegisterClick()
    {
        var register = new RegisterPlayFabUserRequest
        {
            Username = UserName.text,
            Email = Email.text,
            Password = Password.text,
        };

        PlayFabClientAPI.RegisterPlayFabUser    
        (
            register,
            OnRegisterSuccess,
            OnRegisterFailure
        );
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        ErrorMessage.text = "";
        SceneManager.LoadScene(SceneName);
    }

    private void OnRegisterFailure(PlayFabError error)
    {
        if(error.ErrorDetails != null && error.ErrorDetails.Count > 0)
        {
            using (var iterator = error.ErrorDetails.Keys.GetEnumerator())
            {
                iterator.MoveNext();
                string key = iterator.Current;
                ErrorMessage.text = error.ErrorDetails[key][0];
            }
        }
        else
        {
            ErrorMessage.text = error.ErrorMessage;
        }
    }

    public void LoginClick()
    {
        var login = new LoginWithPlayFabRequest
        {
            Username = UserName.text,
            Password = Password.text,
        };

        PlayFabClientAPI.LoginWithPlayFab
        (
            login, 
            OnLoginSuccess,
            OnLoginFailure
        );
    }

    private void OnLoginSuccess(LoginResult result)
    {
        ErrorMessage.text = "";
        SceneManager.LoadScene(SceneName);
    }

    private void OnLoginFailure(PlayFabError error)
    {
        if(error.ErrorDetails != null && error.ErrorDetails.Count > 0)
        {
            using (var iterator = error.ErrorDetails.Keys.GetEnumerator())
            {
                iterator.MoveNext();
                string key = iterator.Current;
                ErrorMessage.text = error.ErrorDetails[key][0];
            }
        }
        else
        {
            ErrorMessage.text = error.ErrorMessage;
        }
    }

}
