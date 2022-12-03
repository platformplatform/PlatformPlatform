param name string
param location string
param tags object

resource networkWatcher 'Microsoft.Network/networkWatchers@2022-05-01' = {
  name: name
  location: location
  tags: tags
}
