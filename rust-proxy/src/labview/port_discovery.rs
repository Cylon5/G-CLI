use std::path::Path;
use ureq::get;
use super::installs::LabviewInstall;
use super::error::LabVIEWError;

pub struct Registration {
    id: String
}

impl Registration {

    pub fn register(vi_path: &Path, install: &LabviewInstall, port: &u16) -> Result<Registration, LabVIEWError> {

        let id = generate_registration_id(vi_path, install);
        // The response we want the discovery service to give. I'm not sure if these need further escaping but so far it works
        let base_response = "HTTP/1.0 200 OK\r\nServer: Service Locator\r\nPragma: no-cache\r\nConnection: Close\r\nContent-Length: 12\r\nContent-Type: text/html\r\n\r\n";
        let url = format!("http://localhost:3580/publish?{}={}Port={}\r\n", id, base_response, port);

        let response = get(&url).call().map_err(|e| LabVIEWError::ServiceLocatorCommsError(e))?;

        let status_code = response.status();

        if status_code > 299 {
            Err(LabVIEWError::ServiceLocatorResponseError(status_code))
        }
        else {
            Ok(Registration {
                id
            })
        }

    }

    /// Unregisters the port with the service locator and consumes the registration object.
    pub fn unregister(self) -> Result<(), LabVIEWError> {
        let response = get(&format!("http://localhost:3580/delete?{}", self.id))
            .call()
            .map_err(|e| LabVIEWError::ServiceLocatorCommsError(e))?;

        let status_code = response.status();

        if status_code > 299 {
            Err(LabVIEWError::ServiceLocatorResponseError(status_code))
        }
        else {
            Ok(())
        }
    }

}

/// Generates an ID unique to the install and VI path.
/// Path should be the full path to the VI.
fn generate_registration_id(vi_path: &Path, install: &LabviewInstall) -> String {

    let path_string = vi_path.to_string_lossy();
    // The extra [..] is required on the pattern array to get the format correct. 
    let reg_name = path_string.replace(&[':','\\','.',' ', '/'][..], "");

    format!("cli/{}/{}/{}", install.major_version(), install.bitness, reg_name)
}


#[cfg(test)]
mod tests {

    use super::*;
    use crate::labview::installs::Bitness;
    use std::path::{Path, PathBuf};

    #[test]
    fn test_builds_the_correct_registration_id_32bit()

    {
        let install = LabviewInstall{
            path: PathBuf::from("C:\\LabVIEW.exe"),
            version: String::from("2011 SP1"),
            bitness: Bitness::X86
        };

        let result = generate_registration_id(Path::new("C:\\myVI.vi"), &install);

        assert_eq!(String::from("cli/2011/32bit/CmyVIvi"), result);

    }

    #[test]
    fn test_builds_the_correct_registration_id_64bit()
    {
        let install = LabviewInstall{
            path: PathBuf::from("C:\\LabVIEW.exe"),
            version: String::from("2011 SP1"),
            bitness: Bitness::X64
        };

        let result = generate_registration_id(Path::new("C:\\myVI.vi"), &install);

        assert_eq!(String::from("cli/2011/64bit/CmyVIvi"), result);

    }

    #[test]
    fn test_builds_the_correct_registration_id_forward_slash_64bit()
    {
        let install = LabviewInstall{
            path: PathBuf::from("C:\\LabVIEW.exe"),
            version: String::from("2011 SP1"),
            bitness: Bitness::X64
        };

        let result = generate_registration_id(Path::new("/C/myVI.vi"), &install);

        assert_eq!(String::from("cli/2011/64bit/CmyVIvi"), result);

    }

}