#!/bin/bash
az network public-ip list --query '[].{tag:tags.service, dns:dnsSettings.fqdn, id:id}[?tag!=null]' -o tsv |
while read -r tag dns id; do
    newdns=(${tag//\// })
    if [ "${newdns[0]}" == "hono" ]; then
    printf 'tag:%s\ndnstobe: %s\nid:%s\n\n\n' "$tag" "${newdns[1]}" "$id"
    az network public-ip update --ids "$id" --dns-name "${newdns[1]}"
    fi
done