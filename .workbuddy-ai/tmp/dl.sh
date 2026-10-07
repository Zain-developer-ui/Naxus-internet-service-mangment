set -e
cd "C:/Users/Zain Ansari/source/repos/Zain-developer-ui/Naxus-internet-service-mangment"
mkdir -p wwwroot/images
declare -A IMG=(
 [fiber-backbone]="photo-1544197150-b99a580bb7a8"
 [data-center]="photo-1558494949-ef010cbdcc31"
 [support-team]="photo-1573164713988-8665fc963095"
 [field-team]="photo-1581092918056-0c4c3acd3789"
 [business-office]="photo-1497366216548-37526070297c"
 [broadband]="photo-1606904825846-647eb07f5be2"
 [wireless]="photo-1573804633927-bfcbcd909acd"
 [home-internet]="photo-1585141445799-4bcd9a18e35c"
 [installation]="photo-1552664730-d307ca884978"
 [network-maintenance]="photo-1597733336794-12d05021d510"
 [plan-basic]="photo-1519389950473-47ba0277781c"
 [router-1]="photo-1544428571-95f3fb8c4f2f"
 [plan-premium]="photo-1517336714731-489689fd1ca8"
 [router-2]="photo-1563770660941-20978e870e26"
 [circuit-board]="photo-1518770660439-4636190af475"
 [billing]="photo-1554224155-6726b3ff858f"
 [team-meeting]="photo-1580894732444-8ecded7900cd"
 [technician]="photo-1581092160607-ee22621dd758"
)
for name in "${!IMG[@]}"; do
  id="${IMG[$name]}"
  curl -s --max-time 40 -o "wwwroot/images/$name.jpg" "https://images.unsplash.com/$id?w=1400&q=80&fm=jpg"
  sz=$(stat -c %s "wwwroot/images/$name.jpg" 2>/dev/null || echo 0)
  echo "$name.jpg -> $sz bytes"
done
