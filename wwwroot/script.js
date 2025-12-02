// DOM이 로드된 후 스크립트 실행
document.addEventListener('DOMContentLoaded', () => {

    // 1. 사용자 행 클릭 시 상세 정보 토글 기능
    const userRows = document.querySelectorAll('.user-row');
    
    userRows.forEach(row => {
        row.addEventListener('click', () => {
            // data-target 속성으로 연결된 ID를 찾음
            const targetId = row.getAttribute('data-target');
            const targetRow = document.getElementById(targetId);
            
            // 'visible' 클래스를 토글하여 행을 보이고 숨김
            if (targetRow) {
                targetRow.classList.toggle('visible');
            }
        });
    });

    // 2. USB 허용/차단 토글 스위치 상태 변경
    const switches = document.querySelectorAll('.switch input[type="checkbox"]');
    
    switches.forEach(sw => {
        sw.addEventListener('change', (e) => {
            const isChecked = e.target.checked;
            const controlDiv = e.target.closest('.control');
            const statusSpan = controlDiv.querySelector('.status-text');

            if (isChecked) {
                statusSpan.textContent = '허용됨';
                statusSpan.className = 'status-text allowed';
                console.log('USB 허용됨'); // 실제로는 서버로 이벤트 전송
            } else {
                statusSpan.textContent = '차단됨';
                statusSpan.className = 'status-text blocked';
                console.log('USB 차단됨'); // 실제로는 서버로 이벤트 전송
            }
        });
    });

    // 3. (시뮬레이션) 실시간 상태 변경
    const statusDots = document.querySelectorAll('.status-dot[id^="status-"]');
    
    setInterval(() => {
        if (statusDots.length === 0) return;
        
        // 랜덤하게 사용자 선택
        const randomDot = statusDots[Math.floor(Math.random() * statusDots.length)];
        
        // 온라인/오프라인 토글
        if (randomDot.classList.contains('online')) {
            randomDot.classList.remove('online');
            randomDot.classList.add('offline');
        } else {
            randomDot.classList.remove('offline');
            randomDot.classList.add('online');
        }
    }, 3000); // 3초마다 상태 변경

});